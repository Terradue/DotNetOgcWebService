using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Terradue.ServiceModel.Ogc;
using Terradue.ServiceModel.Ogc.Exceptions;
using Terradue.ServiceModel.Ogc.Ows11;
using Terradue.WebService.Ogc.Configuration;

namespace Terradue.WebService.Ogc {
    /// <summary>
    /// This class handles HTTP operations that can be used for OGC Service. 
    /// </summary>
    public class OwsHttpRequestHandler : HttpRequestHandler {

        public OwsHttpRequestHandler(IHttpContextAccessor accessor, IMemoryCache cache, IServiceProvider serviceProvider) : base(accessor, cache, serviceProvider) { }

        /// <summary>
        /// Proccesses HTTP request
        /// </summary>
        /// <param name="request">Message with request details</param>
        /// <returns>Response to the request.</returns>
        public override OperationResult ProcessRequest() {

            OperationResult result = null;

            if (this.HttpAccessor == null || this.HttpAccessor.HttpContext == null || this.HttpAccessor.HttpContext.Request == null)
                throw new Exception("Invalid Http context");

            var request = this.HttpAccessor.HttpContext.Request;

            try {
                XDocument doc = null;

                if (request.Headers.ContentLength > 0) {
                    doc = XDocument.Load(request.Body, LoadOptions.None);
                }

                NameValueCollection queryParameters = HttpUtility.ParseQueryString(request.QueryString.Value);

                //  Apply doc or global defaults
                if (queryParameters["service"] == null) {
                    if (doc != null && doc.Root.Attribute("service") != null && !string.IsNullOrEmpty(doc.Root.Attribute("service").Value))
                        queryParameters.Add("service", doc.Root.Attribute("service").Value);
                    else
                        queryParameters.Add("service", ServiceConfiguration.Settings.DefaultService);
                }
                if (queryParameters["version"] == null) {
                    if (doc != null && doc.Root.Attribute("version") != null && !string.IsNullOrEmpty(doc.Root.Attribute("version").Value))
                        queryParameters.Add("version", doc.Root.Attribute("version").Value);
                    else
                        queryParameters.Add("version", ServiceConfiguration.Settings.DefaultVersion);
                }
                if (queryParameters["request"] == null) {
                    if (doc != null && !string.IsNullOrEmpty(doc.Root.Name.LocalName))
                        queryParameters.Add("request", doc.Root.Name.LocalName);
                    else
                        queryParameters.Add("request", ServiceConfiguration.Settings.DefaultRequest);
                }

                var operation = GetServiceOperation(doc, queryParameters);

                //  Apply operation specific defaults
                foreach (var defaultValue in operation.DefaultValues) {
                    if (queryParameters[defaultValue.Name] == null) {
                        queryParameters.Add(defaultValue.Name, defaultValue.DefaultValue);
                    }
                }

                string cacheKey = string.Empty;

                if (operation.CacheEnabled) {
                    //  Create cache key to be used to store results in cache
                    cacheKey = Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(request);

                    if (doc != null) {
                        cacheKey = doc.CreateReader().ReadInnerXml();
                    }

                    //  Get cache results if exists
                    result = this.Cache.Get<OperationResult>(cacheKey);
                }

                if (result == null) {
                    //  Get request handler object for selected operation
                    BaseOperation requestHandler = operation.CreateHandlerInstance(this.HttpAccessor, this.Cache, this.ServiceProvider);

                    OwsRequestBase payload = null;

                    //  If xmlRequest is null then use query parameters to build an request object
                    if (doc == null) {
                        payload = ActivatorUtilities.CreateInstance(this.ServiceProvider, requestHandler.RequestType, new object[] { queryParameters }) as OwsRequestBase;                        
                    } else {
                        XmlSerializer serializer = requestHandler.GetRequestTypeSerializer();
                        payload = serializer.Deserialize(doc.CreateReader()) as OwsRequestBase;
                    }

                    payload.Validate();
                    
                    //  Hanle request and return results back
                    result = requestHandler.ProcessRequest(request, payload);

                    if (result != null && operation.CacheEnabled) {
                        this.Cache.Set<OperationResult>(cacheKey, result);
                    }

                }

                //  Performs special service operation if specified
                if ((from k in queryParameters.AllKeys
                     where k.StartsWith("$$", StringComparison.OrdinalIgnoreCase)
                     select k).Count() > 0)
                    result = HandleCustomAction(result, queryParameters);

            } catch (OgcException exp) {
                //  Handle OGC specific errors
                result = new OperationResult(request) {
                    ResultObject = exp.ExceptionReport,
                };
            } catch (System.Exception exp) {
                //  Handle all other .NET errors
                result = new OperationResult(request) {
                    ResultObject = new NoApplicableCodeException("Application error.", exp).ExceptionReport,
                };
            }

            return result;
        }

        public override Task<OperationResult> ProcessRequestAsync(CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the service operation.
        /// </summary>
        /// <param name="xmlDocument">The XML request.</param>
        /// <param name="queryParameters">The query parameters.</param>
        /// <returns></returns>
        private static ServiceOperationElement GetServiceOperation(XDocument xmlDocument, NameValueCollection queryParameters) {
            string requestName = queryParameters["request"];
            string serviceName = queryParameters["service"];
            string requestVersion;

            //  Validate "request" parameter
            if (string.IsNullOrEmpty(requestName)) {
                throw new MissingParameterValueException("request");
            }

            //  Validate "service" parameter
            if (string.IsNullOrEmpty(serviceName)) {
                throw new MissingParameterValueException("service");
            }

            //  Get version if possible
            if (xmlDocument != null) {
                requestVersion = xmlDocument.Root.Attribute("version").Value;
            } else {
                requestVersion = queryParameters["version"];
            }

            //  Get operation configuration or the default one
            var versionOperations = (from o in ServiceConfiguration.Settings.Services
                                     where
                                        o.Operation == requestName
                                        && o.Service == serviceName
                                         && o.Version.ToVersionNumber() == requestVersion.ToVersionNumber()
                                     select o).ToList();

            var defaultOperation = (from o in ServiceConfiguration.Settings.Services
                                    orderby o.Version descending
                                    where
                                       o.Operation == requestName
                                       && o.Service == serviceName
                                    select o).FirstOrDefault();

            //  Make sure there are no multiple configurations for the service
            if (versionOperations.Count() > 1) {
                throw new ConfigurationErrorsException(string.Format(CultureInfo.InvariantCulture, "Operation '{1}' of service '{0}' has multiple handlers.", serviceName, requestName));
            }

            //  Get a service configuration
            var operation = versionOperations.FirstOrDefault() ?? defaultOperation;

            //  Make sure service configuration is found
            if (operation == null) {
                throw new OperationNotSupportedException(requestName, string.Format(CultureInfo.InvariantCulture, "Operation '{0}' of '{1}' service is not supported.", requestName, serviceName));
            }

            return operation;
        }

        /// <summary>
        /// Handles the custom action specified in URL query path.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="queryParameters">The query parameters.</param>
        /// <returns></returns>
        private static OperationResult HandleCustomAction(OperationResult result, NameValueCollection queryParameters) {
            if (queryParameters.AllKeys.Contains("$$validate")) {
                ExceptionReport report = new ExceptionReport();

                throw new NotImplementedException();
            }
            return result;
        }

    }
}