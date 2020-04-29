using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Terradue.ServiceModel.Ogc.Ows11;
using Terradue.ServiceModel.Ogc.Wps10;

namespace Terradue.WebService.Ogc.Wps.Client
{
	public class WpsClient
	{		
		public Uri WpsServiceUrl { get; }

		public ILogger Logger;

		private static XmlSerializer executeSerializer = new XmlSerializer(
			typeof(Execute));
		private static XmlSerializer executeResponseSerializer = new XmlSerializer(
			typeof(ExecuteResponse));
		private static XmlSerializer exceptionResponseSerializer = new XmlSerializer(
			typeof(ExceptionReport));

		public WpsClient(ILogger logger, Uri wpsServiceUrl)
		{
			Logger = logger;
			WpsServiceUrl = wpsServiceUrl;
		}

		public Task<ExecuteResponse> ExecuteAsync(Execute executeRequest, Action<Task<ExecuteResponse>> onsubmitted, ILogger logger)
		{
			HttpWebRequest httpWebRequest = CreateWpsExecuteWebRequest(executeRequest);

			var task = Task.Run<ExecuteResponse>(() => GetWpsExecuteResponse(httpWebRequest,logger));

			task.ContinueWith(onsubmitted);

			return task;

		}

		public Task<ExecuteResponse> ExecuteAsyncAndPollUntilComplete(ILogger logger, Execute executeRequest, ExecutionState state, Action<StatusType, ExecutionState> onStateChange = null)
		{
			using (var stream = new MemoryStream())
            {
				executeSerializer.Serialize(stream, executeRequest);

                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
					Logger.LogDebug("POST {0}", reader.ReadToEnd());
                }
            }

			HttpWebRequest httpWebRequest = CreateWpsExecuteWebRequest(executeRequest);

			var task = Task.Run<ExecuteResponse>(() =>
			{
				var response = GetWpsExecuteResponse(httpWebRequest, logger);
				return PollUntilComplete(logger, response, state, onStateChange);
			});

			return task;

		}

		private static ExecuteResponse PollUntilComplete(ILogger logger, ExecuteResponse executeResponse, ExecutionState state, Action<StatusType, ExecutionState> onStateChange = null)
		{

			if (executeResponse.Status != null)
			{
				if (executeResponse.Status.Item is ProcessSucceededType || executeResponse.Status.Item is ProcessFailedType)
				{
					return executeResponse;
				}
				if (!executeResponse.Status.Equals(state.Status))
					Task.Run(() => onStateChange(executeResponse.Status, state));
			}

			if (string.IsNullOrEmpty(executeResponse.statusLocation))
				return executeResponse;

			Thread.Sleep(state.PollingPeriod);

			HttpWebRequest httpWebRequest = WebRequest.CreateHttp(new Uri(executeResponse.statusLocation));
			var response = GetWpsExecuteResponse(httpWebRequest, logger);

			return PollUntilComplete(logger, response, state, onStateChange);

		}

		private static ExecuteResponse GetWpsExecuteResponse(HttpWebRequest httpWebRequest, ILogger logger)
		{
			try
			{
				using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					using (Stream stream = httpWebResponse.GetResponseStream())
					{
						var sr = new StreamReader(stream);
						var content = sr.ReadToEnd();
						var strr = new StringReader(content);
						var response = (ExecuteResponse)executeResponseSerializer.Deserialize(strr);
						if (httpWebResponse.StatusCode != HttpStatusCode.OK)
						{
							logger.LogDebug("WPS Execute Response returned an error at {0} : {1}", httpWebRequest.RequestUri, content);
							try
                            {
                                ExceptionReport exceptionReport = (ExceptionReport)exceptionResponseSerializer.Deserialize(strr);
                                throw new InvalidOperationException(string.Format("WPS Execute Response at {0} returned an exception report. {1}", httpWebRequest.RequestUri, exceptionReport.Exceptions.First().ExceptionText));
                            }
                            catch (Exception e2)
                            {
                                throw e2;
                            }
						}
						if (response != null)
							return response;

					}
				}
			}
			catch (Exception e)
			{
				logger.LogError("Error reading the execute response at {1} : {0}", e.Message, httpWebRequest.RequestUri);
				logger.LogDebug(e.StackTrace);
				throw;
			}

			return null;

		}

		private HttpWebRequest CreateWpsExecuteWebRequest(Execute executeRequest)
		{
			// ** temp
			//HttpWebRequest httpWebRequest = WebRequest.CreateHttp(new Uri("http://10.15.34.126:8080/wps/RetrieveResultServlet?id=684fc957-5280-4331-be18-36d035fe2f92"));
			// **
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
			HttpWebRequest httpWebRequest = WebRequest.CreateHttp(WpsServiceUrl);
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/xml";
			httpWebRequest.Accept = "application/xml";
			Stream stream = httpWebRequest.GetRequestStream();
			executeSerializer.Serialize(stream, executeRequest);

			return httpWebRequest;
		}
	}
}
