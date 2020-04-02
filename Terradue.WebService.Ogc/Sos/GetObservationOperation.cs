using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Terradue.ServiceModel.Ogc;
using Terradue.ServiceModel.Ogc.Ows11;
using Terradue.WebService.Ogc.Common;
using Terradue.WebService.Ogc.Configuration;

namespace Terradue.WebService.Ogc.Sos {
    /// <summary>
    /// Represents a sample GetObservation request handler
    /// </summary>
    public class GetObservationOperation : BaseSosOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetObservationOperation"/> class.
        /// </summary>
        /// <param name="configuration">Operation configuration.</param>
        public GetObservationOperation(ServiceOperationElement configuration, SosEntitiesFactory entitiesFactory, IHttpContextAccessor accessor, IMemoryCache cache)
            : base(configuration, entitiesFactory, accessor, cache)
        {
        }

        private IDictionary<System.Net.Mime.ContentType, BaseObservationFormatter> _responseFormatHandlers;

        /// <summary>
        /// Gets the response format handlers.
        /// </summary>
        /// <value>The response format handlers.</value>
        public IDictionary<System.Net.Mime.ContentType, BaseObservationFormatter> ResponseFormatHandlers
        {
            get
            {
                if (_responseFormatHandlers == null)
                {
                    _responseFormatHandlers = new Dictionary<ContentType, BaseObservationFormatter>();
                    foreach (var responseFormatter in this.Configuration.ServiceConfiguration.ResponseFormatters)
                    {
                        var type = Type.GetType(responseFormatter.HandlerType);
                        
                        if (type == null)
                        {
                            throw new ConfigurationErrorsException(string.Format(CultureInfo.InvariantCulture, "Formatter type '{0}' is invalid or not found.", responseFormatter.HandlerType));
                        }

                        _responseFormatHandlers.Add(new ContentType(responseFormatter.ContentType), Activator.CreateInstance(type, new object[] { this }) as BaseObservationFormatter);
                    }
                }
                return _responseFormatHandlers.ToReadOnly();
            }
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
                    Name = "procedure",
                    AllowedValues = new Collection<object>(),
                });

                DomainType parameter = parameters[parameters.Count - 1];

                foreach (var sensorName in this.UrnManager.SensorNames)
                {
                    parameter.AllowedValues.Add(new Terradue.ServiceModel.Ogc.Ows11.ValueType(this.UrnManager.GetSensorUrn(sensorName).ToString()));
                }

                parameters.Add(new DomainType()
                {
                    Name = "offering",
                    AnyValue = new AnyValue(),
                    Meaning = new DomainMetadataType
                    {
                        Value = "Valid offering name",
                    }
                });



                parameters.Add(new DomainType()
                {
                    Name = "observedProperty",
                    AllowedValues = new Collection<object>(),
                });

                parameter = parameters[parameters.Count - 1];

                foreach (var observedProperty in this.UrnManager.ObservedPropertyNames)
                {
                    parameter.AllowedValues.Add(new Terradue.ServiceModel.Ogc.Ows11.ValueType(this.UrnManager.GetPropertyUrn(observedProperty).ToString()));
                }

                parameters.Add(new DomainType()
                {
                    Name = "temporalFilter",
                    AnyValue = new AnyValue(),
                    Meaning = new DomainMetadataType
                    {
                        Value = "Valid event time. For example: 2008-11-06T18:50:00Z or to specify range 2009-01-01/2009-02-01",
                    }
                });

                parameters.Add(new DomainType()
                {
                    Name = "featureOfInterest",
                    AllowedValues = new Collection<object>(),
                });

                parameter = parameters[parameters.Count - 1];

                foreach (var featureOfInterest in this.UrnManager.FeatureOfInterestNames)
                {
                    parameter.AllowedValues.Add(new Terradue.ServiceModel.Ogc.Ows11.ValueType(this.UrnManager.GetFeatureOfInterestUrn(featureOfInterest).ToString()));
                }

                parameters.Add(new DomainType()
                {
                    Name = "spatialFilter",
                    AnyValue = new AnyValue(),
                    Meaning = new DomainMetadataType
                    {
                        Value = "Valid spatial operator according to OGC 09-026r1",
                    }
                });

                parameters.Add(new DomainType()
                {
                    Name = "responseFormat",
                    AllowedValues = new Collection<object>(),
                });

                parameter = parameters[parameters.Count - 1];

                foreach (var code in this.ResponseFormatHandlers.Keys)
                {
                    parameter.AllowedValues.Add(new Terradue.ServiceModel.Ogc.Ows11.ValueType(code.ToString()));
                }


                return parameters;
            }
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
                dcp.Http.PostMethods.Add(new RequestMethodType()
                {
                    Href = string.Format(CultureInfo.InvariantCulture, "{0}/", this.ServiceBaseUri)
                });
                dcp.Http.GetMethods.Add(new RequestMethodType()
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
            get { return null; }
        }

        /// <summary>
        /// Gets operation metadata list for GetObservation operation
        /// </summary>
        public override Collection<MetadataType> OperationMetadata
        {
            get { return null; }
        }

        /// <summary>
        /// Gets a type of request object to be used for current operation
        /// </summary>
        public override Type RequestType
        {
            get { return typeof(Terradue.ServiceModel.Ogc.Sos20.GetObservationType); }
        }

        /// <summary>
        /// Process operation request
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Resposne object to be sent back to the client</returns>
        public override OperationResult ProcessRequest(HttpRequest request, OwsRequestBase payload = null)
        {
            throw new NotImplementedException();
        }

        #endregion


        #region Helper classes

        private class ObservationValuesSummary
        {
            public string ObservationCode { get; set; }
            public int Total { get; set; }
            public DateTime SamplingBeginDate { get; set; }
            public DateTime SamplingEndDate { get; set; }
        }

        #endregion
    }
}
