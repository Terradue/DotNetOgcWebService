using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using Terradue.ServiceModel.Ogc;
using Terradue.ServiceModel.Ogc.Ows11;
using Terradue.WebService.Ogc.WebService.Common;
using Terradue.WebService.Ogc.Configuration;
using Terradue.Core.Process;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Terradue.WebService.Ogc.Wps
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseWpsOperation : Terradue.WebService.Ogc.BaseOperation
    {
        /// <summary>
        /// Used for thread locking
        /// </summary>
        private static object _lock = new object();

        private IDictionary<string, WpsProcess> _processes;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseWpsOperation"/> class.
        /// </summary>
        /// <param name="configuration">Operation configuration.</param>
        protected BaseWpsOperation(ServiceOperationElement configuration, IHttpContextAccessor accessor, IMemoryCache cache)
            : base(configuration, accessor, cache)
        {
        }

        /// <summary>
        /// Creates and gets a new instance of <see cref="SosEntities"/>.
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, WpsProcess> GetProcesses()
        {
            if (_processes == null)
                _processes = LoadProcessesFromConfiguration();
            return _processes;
        }

        #region BaseOperation abstract methods

        /// <summary>
        /// Gets operation parameters list for GetObservation operation
        /// </summary>
        public override abstract Collection<DomainType> OperationParameters { get; }

        /// <summary>
        /// Gets operation dcp list for GetObservation operation
        /// </summary>
        public override abstract Collection<Dcp> OperationDcps { get; }

        /// <summary>
        /// Gets operation constraints list for GetObservation operation
        /// </summary>
        public override abstract Collection<DomainType> OperationConstraints { get; }

        /// <summary>
        /// Gets operation metadata list for GetObservation operation
        /// </summary>
        public override abstract Collection<MetadataType> OperationMetadata { get; }

        /// <summary>
        /// Gets a type of request object to be used for current operation
        /// </summary>
        public override abstract Type RequestType { get; }

        /// <summary>
        /// Process request
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Resposne object to be sent back to the client</returns>
        public override abstract OperationResult ProcessRequest(HttpRequestMessage request, OwsRequestBase payload = null);

        #endregion

        IDictionary<string, WpsProcess> LoadProcessesFromConfiguration()
        {
            IDictionary<string, WpsProcess> processes = new Dictionary<string, WpsProcess>();

            foreach (var processConfig in WebProcessingServiceConfiguration.Settings.Processes)
            {
                WpsProcess process = processConfig.CreateHandlerInstance();
                processes.Add(process.Id, process);
            }

            return processes;
        }
    }
}
