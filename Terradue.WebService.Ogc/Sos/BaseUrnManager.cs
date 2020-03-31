using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Terradue.WebService.Ogc.WebService.Common;

namespace Terradue.WebService.Ogc.Sos {
    /// <summary>
    /// Represnts base class for Urn managment
    /// </summary>
    public abstract class BaseUrnManager
    {
        /// <summary>
        /// Gets the cache timeout.
        /// </summary>
        /// <value>The cache timeout.</value>
        public int CacheTimeout
        {
            get
            {
                return this.Operation.Configuration.ServiceConfiguration.CacheTimeout;
            }
        }

        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <value>The operation.</value>
        public BaseSosOperation Operation { get; private set; }

        protected readonly IMemoryCache Cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseUrnManager"/> class.
        /// </summary>
        /// <param name="operation">The operation.</param>
        protected BaseUrnManager(BaseSosOperation operation, IMemoryCache cache)
        {
            this.Operation = operation;
            this.Cache = cache;
        }

        /// <summary>
        /// Used for thread locking
        /// </summary>
        private static object _lock = new object();

        /// <summary>
        /// Holds urn string to value reference
        /// </summary>
        private IDictionary<Uri, string> _urnLookup;
        /// <summary>
        /// Gets the urn lookup.
        /// </summary>
        /// <value>The urn lookup.</value>
        protected IDictionary<Uri, string> UrnLookup
        {
            get
            {
                if (this._urnLookup == null)
                {
                    this._urnLookup = this.Cache.Get<IDictionary<Uri, string>>("__URN_LOOKUP");

                    if (this._urnLookup == null)
                    {
                        this._urnLookup = new Dictionary<Uri, string>();

                        //  Add sensor values to lookup table
                        this._urnLookup = this._urnLookup.Union(UrnSensorNames.ToDictionary(a => a.Value, a => a.Key)).ToDictionary(x => x.Key, x => x.Value);

                        //  Add property values to lookup table
                        this._urnLookup = this._urnLookup.Union(UrnObservedPropertyNames.ToDictionary(a => a.Value, a => a.Key)).ToDictionary(x => x.Key, x => x.Value); ;

                        //  Add feature of interest values to lookup table
                        this._urnLookup = this._urnLookup.Union(UrnFeatureOfInterestNames.ToDictionary(a => a.Value, a => a.Key)).ToDictionary(x => x.Key, x => x.Value); ;

                        this.Cache.Set<IDictionary<Uri, string>>("__URN_LOOKUP", this._urnLookup,TimeSpan.FromMinutes(this.CacheTimeout));
                    }
                }
                return this._urnLookup;
            }
        }

        private IDictionary<string, Uri> _urnSensorNames;
        /// <summary>
        /// Gets dictionary which maps sensor names to urn names
        /// </summary>
        /// <value>The urn sensor names.</value>
        protected IDictionary<string, Uri> UrnSensorNames
        {
            get
            {
                if (_urnSensorNames == null)
                {
                    this._urnSensorNames = this.Cache.Get<IDictionary<string, Uri>>("__URN_SENSORNAMES");

                    if (_urnSensorNames == null)
                    {
                        IDictionary<string, ServiceModel.Ogc.SensorMl20.DescribedObjectType> sensors = this.Operation.SosEntitiesFactory.GetSensors();

                        lock (_lock)
                        {
                            if (_urnSensorNames == null)
                            {
                                _urnSensorNames = (from s in sensors
                                                   select s).ToDictionary(x => x.Key, x => this.GetSensorUrn(x.Value.identifier.Value));
                            }
                        }

                        this.Cache.Set<IDictionary<string, Uri>>("__URN_SENSORNAMES", this._urnSensorNames, TimeSpan.FromMinutes(this.CacheTimeout));
                    }
                }
                return _urnSensorNames.ToReadOnly();
            }
        }

        private IDictionary<string, Uri> _urnObservedPropertyNames;
        /// <summary>
        /// Gets dictionary which maps sensor names to urn names
        /// </summary>
        /// <value>The urn sensor names.</value>
        protected IDictionary<string, Uri> UrnObservedPropertyNames
        {
            get
            {
                if (_urnObservedPropertyNames == null)
                {
                    this._urnObservedPropertyNames = this.Cache.Get<IDictionary<string, Uri>>("__URN_OBSERVEDPROPERTYNAMES");

                    if (_urnObservedPropertyNames == null)
                    {
                        IDictionary<string, ServiceModel.Ogc.Gml321.ReferenceType> properties = this.Operation.SosEntitiesFactory.GetObservedProperties();

                        lock (_lock)
                        {
                            if (_urnObservedPropertyNames == null)
                            {
                                _urnObservedPropertyNames = (from s in properties
                                                             select s).ToDictionary(x => x.Key, x => this.GetPropertyUrn(x.Value.remoteSchema));
                            }
                        }

                        this.Cache.Set<IDictionary<string, Uri>>("__URN_OBSERVEDPROPERTYNAMES", this._urnObservedPropertyNames, TimeSpan.FromMinutes(this.CacheTimeout));
                    }
                }
                return _urnObservedPropertyNames.ToReadOnly();
            }
        }

        private IDictionary<string, Uri> _urnFeatureOfInterestNames;
        /// <summary>
        /// Gets dictionary which maps sensor names to urn names
        /// </summary>
        /// <value>The urn sensor names.</value>
        protected IDictionary<string, Uri> UrnFeatureOfInterestNames
        {
            get
            {
                if (_urnFeatureOfInterestNames == null)
                {
                    this._urnFeatureOfInterestNames = this.Cache.Get<IDictionary<string, Uri>>("__URN_FEATUREOFINTERESTNAMES");

                    if (_urnFeatureOfInterestNames == null)
                    {
                        IDictionary<string, ServiceModel.Ogc.Gml321.FeaturePropertyType> result = this.Operation.SosEntitiesFactory.GetFeaturesOfInterest();

                        lock (_lock)
                        {
                            if (_urnFeatureOfInterestNames == null)
                            {
                                _urnFeatureOfInterestNames = (from s in result
                                                              select s).ToDictionary(x => x.Key, x => this.GetFeatureOfInterestUrn(x.Value.remoteSchema));
                            }
                        }

                        this.Cache.Set<IDictionary<string, Uri>>("__URN_FEATUREOFINTERESTNAMES", this._urnFeatureOfInterestNames, TimeSpan.FromMinutes(this.CacheTimeout));
                    }
                }
                return _urnFeatureOfInterestNames.ToReadOnly();
            }
        }

        /// <summary>
        /// Gets list of the sensor names.
        /// </summary>
        /// <value>The sensor names.</value>
        public ICollection<string> SensorNames
        {
            get
            {
                return this.UrnSensorNames.Keys;
            }
        }

        /// <summary>
        /// Gets list of the observed properties.
        /// </summary>
        /// <value>The observed properties.</value>
        public ICollection<string> ObservedPropertyNames
        {
            get
            {
                return this.UrnObservedPropertyNames.Keys;
            }
        }

        /// <summary>
        /// Gets list of the features of interest.
        /// </summary>
        /// <value>The features of interest.</value>
        public ICollection<string> FeatureOfInterestNames
        {
            get
            {
                return this.UrnFeatureOfInterestNames.Keys;
            }
        }

        /// <summary>
        /// Gets urn property name
        /// </summary>
        /// <param name="value">Property name</param>
        /// <returns>Property urn string</returns>
        public virtual Uri GetPropertyUrn(string value)
        {
            return this.GetUrnName("property", value);
        }

        /// <summary>
        /// Gets urn sensor name
        /// </summary>
        /// <param name="value">Sensor name</param>
        /// <returns>Sensor urn string</returns>
        public virtual Uri GetSensorUrn(string value)
        {
            return this.GetUrnName("sensor", value);
        }

        /// <summary>
        /// Gets urn feature of interest name
        /// </summary>
        /// <param name="value">Feature of interest name</param>
        /// <returns>Feature of interest urn string</returns>
        public virtual Uri GetFeatureOfInterestUrn(string value)
        {
            return this.GetUrnName("featureOfInterest", value);
        }

        /// <summary>
        /// Gets urn string for specified name and value.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public abstract Uri GetUrnName(string name, string value);

        /// <summary>
        /// Gets the urn string value.
        /// </summary>
        /// <param name="urn">The urn.</param>
        /// <returns></returns>
        public virtual string GetUrnValue(Uri urn)
        {
            if (this.UrnLookup.ContainsKey(urn))
                return this.UrnLookup[urn];
            else
                return null;
        }
    }
}
