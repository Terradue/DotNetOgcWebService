using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Terradue.ServiceModel.Ogc;
using Terradue.ServiceModel.Ogc.Exceptions;
using Terradue.ServiceModel.Ogc.Ows11;
using Terradue.ServiceModel.Ogc.Wps10;
using Terradue.WebService.Ogc.Configuration;

namespace Terradue.WebService.Ogc.Wps {
    /// <summary>
    /// Represents a sample DescribeSensor request handler
    /// </summary>
    public class ExecuteOperation : BaseWpsOperation
    {

        private static XmlSerializer describedObjectTypeSerializer = new XmlSerializer(typeof(ProcessDescriptionType));


        private IDictionary<string, Func<ProcessDescriptionType, object>> _outputFormatters;
        /// <summary>
        /// Gets output formatters list
        /// </summary>
        public virtual IDictionary<string, Func<ProcessDescriptionType, object>> OutputFormatters
        {
            get
            {
                if (this._outputFormatters == null)
                {
                    this._outputFormatters = new Dictionary<string, Func<ProcessDescriptionType, object>>();
                }

                return this._outputFormatters;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DescribeSensorOperation"/> class.
        /// </summary>
        /// <param name="configuration">Operation configuration.</param>
        public ExecuteOperation(ServiceOperationElement configuration, IHttpContextAccessor accessor, IMemoryCache cache, HttpClient httpClient, ILogger logger)
            : base(configuration, accessor, cache, httpClient, logger)
        {
        }

        #region BaseOperation abstract methods

        /// <summary>
        /// Gets operation parameters list for GetObservation operation
        /// </summary>
        public override Collection<DomainType> OperationParameters
        {
            get
            {
                Collection<DomainType> parameters = new Collection<DomainType>();

                parameters.Add(new DomainType()
                {
                    Name = "Identifier",
                    AllowedValues = new Collection<object>(),
                });

                DomainType parameter = parameters[parameters.Count - 1];

                foreach (var process in this.GetProcesses())
                {
                    parameter.AllowedValues.Add(new Terradue.ServiceModel.Ogc.Ows11.ValueType(process.Key));
                }

                parameters.Add(new DomainType()
                {
                    Name = "Language",
                    AllowedValues = new Collection<object>(),
                });

                if (this.OutputFormatters.Count > 0)
                {
                    parameter = parameters[parameters.Count - 1];

                    foreach (var format in this.OutputFormatters.Keys)
                    {
                        parameter.AllowedValues.Add(new Terradue.ServiceModel.Ogc.Ows11.ValueType(format));
                    }
                }

                return parameters;
            }
        }

        /// <summary>
        /// Gets a type of request object to be used for current operation
        /// </summary>
        public override Type RequestType
        {
            get { return typeof(Execute); }
        }

		/// <summary>
		/// Gets operation dcp list for GetObservation operation
		/// </summary>
		public override Collection<Dcp> OperationDcps
		{
			get
			{
				Collection<Dcp> dcps = new Collection<Dcp>();
				Dcp dcp = new Dcp();
				dcp.Http.PostMethods.Add(new RequestMethodType
				{
					Href = string.Format(CultureInfo.InvariantCulture, "{0}/", this.ServiceBaseUri)
				});
				dcp.Http.GetMethods.Add(new RequestMethodType
				{
					Href = string.Format(CultureInfo.InvariantCulture, "{0}/", this.ServiceBaseUri)
				});

				dcps.Add(dcp);

				return dcps;
			}
		}

		/// <summary>
		/// Gets operation constraints list for GetObservation operation
		/// </summary>
		public override Collection<DomainType> OperationConstraints
		{
			get
			{
				Collection<DomainType> constraints = new Collection<DomainType>();

				return constraints;
			}
		}

		/// <summary>
		/// Gets operation metadata list for GetObservation operation
		/// </summary>
		public override Collection<MetadataType> OperationMetadata
		{
			get { return null; }
		}

		/// <summary>
		/// Process operation request
		/// </summary>
		/// <param name="request"></param>
		/// <returns>Resposne object to be sent back to the client</returns>
		public override OperationResult ProcessRequest(HttpRequest request, OwsRequestBase payload = null)
        {

            this.Logger.LogInformation("Process Execute request");

            string identifier = ((Execute)payload).Identifier;
            this.Logger.LogDebug("identifier = {0}",identifier);

            //  Make sure there is valid request parameter
            if (string.IsNullOrEmpty(identifier))
            {
                throw new NoApplicableCodeException(string.Format(CultureInfo.CurrentCulture, "Process identifier is mandatory for Execute operation.", identifier));
            }

            OperationResult result = new OperationResult(request);

            var processes = this.GetProcesses();

            if (!processes.ContainsKey(identifier)) 
            {
                throw new InvalidParameterValueException("identifier", identifier);
            }

            var process = processes[identifier];
            process.SetHttpClient(this.HttpClient);
            process.SetMemoryCache(this.Cache);

            ExecuteResponse executeResponse = process.SubmitExecuteProcess(payload as Execute);

            result.ResultObject = executeResponse;

            return result;
        }

		#endregion
	}
}
