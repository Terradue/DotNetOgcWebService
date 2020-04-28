using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Terradue.ServiceModel.Ogc;
using Terradue.ServiceModel.Ogc.Ows11;
using Terradue.WebService.Ogc.Configuration;

namespace Terradue.WebService.Ogc.Sos {
    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseSosOperation : Terradue.WebService.Ogc.BaseOperation
    {
        /// <summary>
        /// Used for thread locking
        /// </summary>
        private static object _lock = new object();

        private BaseUrnManager _urnManager;
        /// <summary>
        /// Gets the urn manager.
        /// </summary>
        /// <value>The urn manager.</value>
        public BaseUrnManager UrnManager
        {
            get
            {
                if (_urnManager == null)
                {
                    this._urnManager = this.CreateManager("UrnManager", new DefaultUrnManager(this, this.Cache)) as BaseUrnManager;
                }
                return this._urnManager;
            }
        }

        private BaseUriManager _uriManager;
        /// <summary>
        /// Gets the URI manager.
        /// </summary>
        /// <value>The URI manager.</value>
        public BaseUriManager UriManager
        {
            get
            {
                if (_uriManager == null)
                {
                    this._uriManager = this.CreateManager("UriManager", new DefaultUriManager(this)) as BaseUriManager;
                }
                return this._uriManager;
            }
        }

        readonly SosEntitiesFactory entitiesFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseSosOperation"/> class.
        /// </summary>
        /// <param name="configuration">Operation configuration.</param>
        protected BaseSosOperation(ServiceOperationElement configuration, SosEntitiesFactory entitiesFactory, IHttpContextAccessor accessor, IMemoryCache cache, HttpClient httpClient)
            : base(configuration, accessor, cache, httpClient)
        {
            this.entitiesFactory = entitiesFactory;
        }

        /// <summary>
        /// Creates and gets a new instance of <see cref="SosEntities"/>.
        /// </summary>
        /// <returns></returns>
        public SosEntitiesFactory SosEntitiesFactory
        {
            get
            {
                return entitiesFactory;
            }
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
        public override abstract OperationResult ProcessRequest(HttpRequest request, OwsRequestBase payload = null);

        #endregion

        /// <summary>
        /// Creates an instance of the specified manager.
        /// </summary>
        /// <param name="managerName">Name of the manager.</param>
        /// <param name="defaultManager">The default manager.</param>
        /// <returns></returns>
        private object CreateManager(string managerName, object defaultManager)
        {
            object manager = null;

            var managerTypeName = (from c in this.Configuration.ServiceConfiguration.ServiceManagers
                                   where c.Name.Equals(managerName, StringComparison.OrdinalIgnoreCase)
                                   select c.ManagerType).SingleOrDefault();

            if (string.IsNullOrEmpty(managerTypeName))
            {
                //  Create default manager
                manager = defaultManager;
            }
            else
            {
                var managerType = Type.GetType(managerTypeName);

                if (managerType == null)
                {
                    throw new ConfigurationErrorsException(string.Format(CultureInfo.InvariantCulture, "Manager '{0}' is not valid. Cannot create type '{1}'.", managerName, managerTypeName));
                }
                manager = Activator.CreateInstance(managerType, new object[] { this });
            }
            return manager;
        }
    }
}
