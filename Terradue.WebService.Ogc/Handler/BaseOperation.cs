using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Terradue.ServiceModel.Ogc;
using Terradue.ServiceModel.Ogc.Ows11;
using Terradue.WebService.Ogc.Common;
using Terradue.WebService.Ogc.Configuration;

namespace Terradue.WebService.Ogc {
    /// <summary>
    /// Base class for all RequestHandler implementions.
    /// </summary>
    public abstract class BaseOperation {

        protected readonly IHttpContextAccessor Accessor;
        protected readonly IMemoryCache Cache;
        protected readonly HttpClient HttpClient;
        protected readonly ILogger Logger;

        /// <summary>
        /// Gets an operation configuration information
        /// </summary>
        public ServiceOperationElement Configuration { get; private set; }

        /// <summary>
        /// Gets an operation service name
        /// </summary>
        public string ServiceName { get; private set; }

        /// <summary>
        /// Gets an operation request name
        /// </summary>
        public string RequestName { get; private set; }

        /// <summary>
        /// Gets an operation version
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseOperation"/> class.
        /// </summary>
        /// <param name="configuration">Operation configuration.</param>
        protected BaseOperation(ServiceOperationElement configuration, IHttpContextAccessor accessor, IMemoryCache cache, HttpClient httpClient, ILogger logger)
        {
            this.Configuration = configuration;

            this.ServiceName = this.Configuration.Service;
            this.RequestName = this.Configuration.Operation;
            this.Version = this.Configuration.Version;

            this.Accessor = accessor;
            this.Cache = cache;
            this.HttpClient = httpClient;
            this.Logger = logger;

            var url = UriHelper.GetDisplayUrl(this.Accessor.HttpContext.Request);
            this.Logger.LogDebug("BaseOperation constructor, uri = {0}", url);

            //  Set service base uri
            this.ServiceBaseUri = new Uri(url);
        }

        /// <summary>
        /// Gets service base uri
        /// </summary>
        /// <remarks>
        /// Since this property usses current context to initlize its value it must be done before any thread calls this property get method
        /// </remarks>
        public Uri ServiceBaseUri
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets list of configured services
        /// </summary>
        public ConfigurationCollection<ServiceOperationElement> Services
        {
            get
            {
                return ServiceConfiguration.Settings.Services;
            }
        }

        /// <summary>
        /// Get Request type Xml Serializer
        /// </summary>
        /// <returns></returns>
        public XmlSerializer GetRequestTypeSerializer() {
            return this.RequestType.GetSerializer();
        }

        /// <summary>
        /// Gets operation parameters list for GetObservation operation
        /// </summary>
        public abstract Collection<DomainType> OperationParameters { get; }

        /// <summary>
        /// Gets operation dcp list for GetObservation operation
        /// </summary>
        public abstract Collection<Dcp> OperationDcps { get; }

        /// <summary>
        /// Gets operation constraints list for GetObservation operation
        /// </summary>
        public abstract Collection<DomainType> OperationConstraints { get; }

        /// <summary>
        /// Gets operation metadata list for GetObservation operation
        /// </summary>
        public abstract Collection<MetadataType> OperationMetadata { get; }

        /// <summary>
        /// Gets a type of request object to be used for current operation
        /// </summary>
        public abstract Type RequestType { get; }

        /// <summary>
        /// Process request
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Resposne object to be sent back to the client</returns>
        public abstract OperationResult ProcessRequest(HttpRequest request, OwsRequestBase payload = null);
    }
}
