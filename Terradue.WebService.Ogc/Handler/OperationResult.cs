using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Terradue.ServiceModel.Ogc;
using Terradue.ServiceModel.Ogc.Exceptions;
using Terradue.WebService.Ogc.Common;

namespace Terradue.WebService.Ogc {
    /// <summary>
    /// Used to return result of an operation.
    /// </summary>
    /// <remarks>
    /// This class is responsible to create object of type <see cref="Message"/> that will be used to return results from the service.
    /// </remarks>
    public class OperationResult : IActionResult
    {
        readonly HttpRequest _request;

        public OperationResult() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationResult"/> class.
        /// </summary>
        public OperationResult(HttpRequest request)
        {
            this._request = request;
            this.OutputFormat = OutputFormat.ApplicationXml;
        }

        /// <summary>
        /// Gets or sets result of the operation
        /// </summary>
        public object ResultObject { get; set; }

        /// <summary>
        /// Gets or sets output MIME format to be used to return result
        /// </summary>
        public OutputFormat OutputFormat { get; set; }

        /// <summary>
        /// Gets message object to be returned by the service as the result of the operation
        /// </summary>
        /// <returns></returns>        
        public Task ExecuteResultAsync(ActionContext context) {
            HttpResponseMessage result = new HttpResponseMessage();
            
            switch (this.OutputFormat) {
                case OutputFormat.ApplicationXmlExternalParsedEntity:
                case OutputFormat.TextXmlExternalParsedEntity:
                case OutputFormat.ApplicationXml:
                case OutputFormat.TextXml:
                case OutputFormat.ApplicationXmlWaterMl2: {
                        result.Content = new XmlContent(this.ResultObject, this.OutputFormat.ToStringValue());
                        result.RequestMessage = new HttpRequestMessage(new HttpMethod(_request.Method), Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(_request));
                        break;
                    }

                case OutputFormat.TextCsv: {
                        if (this.ResultObject is IEnumerable<object>) {
                            using (var memoryStream = new MemoryStream())
                            using (var streamWriter = new StreamWriter(memoryStream))
                            using (var csvWriter = new CsvHelper.CsvWriter(streamWriter,CultureInfo.InvariantCulture)) {
                                var records = this.ResultObject as IEnumerable<object>;
                                csvWriter.WriteRecords(records);
                                streamWriter.Flush();
                                memoryStream.Position = 0;
                                result.Content = new StreamContent(memoryStream);
                                result.RequestMessage = new HttpRequestMessage(new HttpMethod(_request.Method), Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(_request));                                
                                //result.ContentType = new MediaTypeHeaderValue("text/csv").MediaType;
                            }
                        } else {
                            throw new NoApplicableCodeException("Message is empty or not set or result object is not supported.");
                        }
                        break;
                    }

                case OutputFormat.TextPlain: {
                        string message = string.Format(CultureInfo.InvariantCulture, "{0}", this.ResultObject);

                        if (string.IsNullOrEmpty(message)) {
                            throw new NoApplicableCodeException("Message is empty or not set or result object is not supported.");
                        }

                        result.Content = new StringContent(message);                            
                        break;
                    }
                default:
                    break;
            }

            return Task.FromResult(result); ;
        }
    }
}
