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
using Terradue.ServiceModel.Ogc.SensorMl20;
using Terradue.WebService.Ogc.Configuration;

namespace Terradue.WebService.Ogc.Sos {
    /// <summary>
    /// Represents a sample DescribeSensor request handler
    /// </summary>
    public class DescribeSensorOperation : BaseSosOperation
    {

        private static XmlSerializer describedObjectTypeSerializer = new XmlSerializer(typeof(DescribedObjectType));


        private IDictionary<string, Func<DescribedObjectType, object>> _outputFormatters;
        /// <summary>
        /// Gets output formatters list
        /// </summary>
        public virtual IDictionary<string, Func<DescribedObjectType, object>> OutputFormatters
        {
            get
            {
                if (this._outputFormatters == null)
                {
                    this._outputFormatters = new Dictionary<string, Func<DescribedObjectType, object>>();
                    this._outputFormatters.Add("http://www.opengis.net/sensorML/2.0.0\"", this.GetSensorML);
                }

                return this._outputFormatters;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DescribeSensorOperation"/> class.
        /// </summary>
        /// <param name="configuration">Operation configuration.</param>
        public DescribeSensorOperation(ServiceOperationElement configuration, SosEntitiesFactory entitiesFactory, IHttpContextAccessor accessor, IMemoryCache cache, IServiceProvider serviceProvider)
            : base(configuration, entitiesFactory, accessor, cache, serviceProvider)
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
                    Name = "SensorId",
                    AllowedValues = new Collection<object>(),
                });

                DomainType parameter = parameters[parameters.Count - 1];

                foreach (var sensorName in this.UrnManager.SensorNames)
                {
                    parameter.AllowedValues.Add(new Terradue.ServiceModel.Ogc.Ows11.ValueType(this.UrnManager.GetSensorUrn(sensorName).ToString()));
                }

                parameters.Add(new DomainType()
                {
                    Name = "outputFormat",
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

                constraints.Add(new DomainType()
                {
                    Name = "SupportedSensorDescription",
                    AllowedValues = new Collection<object>()
                        {
                            new Terradue.ServiceModel.Ogc.Ows11.ValueType("sml:SensorML"),
                        },
                    Meaning = new DomainMetadataType()
                    {
                        Reference = this.UrnManager.GetPropertyUrn("SupportedSensorDescription").ToString(),
                        Value = "The service will only accept sensor descriptions that comply with the listed ones.",
                    }
                });

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
        /// Gets a type of request object to be used for current operation
        /// </summary>
        public override Type RequestType
        {
            get { return typeof(Terradue.ServiceModel.Ogc.Swes20.DescribeSensorType); }
        }

        /// <summary>
        /// Process operation request
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Resposne object to be sent back to the client</returns>
        public override OperationResult ProcessRequest(HttpRequest request, OwsRequestBase payload = null)
        {
            Terradue.ServiceModel.Ogc.Swes20.DescribeSensorType dsr = payload as Terradue.ServiceModel.Ogc.Swes20.DescribeSensorType;

            //  Make sure there is valid request parameter
            if (dsr == null)
            {
                throw new NoApplicableCodeException(string.Format(CultureInfo.CurrentCulture, "Type '{0}' is not supported by DescribeRequest operation.", payload.GetType().Name));
            }

            OperationResult result = new OperationResult(request);

            var sensor = this.SosEntitiesFactory.GetSensors();

            if (!sensor.ContainsKey(dsr.procedure)) 
            {
                throw new InvalidParameterValueException("procedure", dsr.procedure);
            }

            //  Get appropriate output formatter and execute it if available
            if (this.OutputFormatters.ContainsKey(dsr.procedureDescriptionFormat))
            {
                result.ResultObject = this.OutputFormatters[dsr.procedureDescriptionFormat](sensor[dsr.procedure]);
            }
            else
            {
                throw new InvalidParameterValueException("outputFormat", dsr.procedureDescriptionFormat);
            }

            return result;
        }

        #endregion

        #region SensorML methods

        /// <summary>
        /// Gets SensorML object for provided sensor information
        /// </summary>
        /// <param name="sensorInfo"></param>
        /// <returns></returns>
        protected virtual Terradue.ServiceModel.Ogc.SensorMl20.DescribedObjectType GetSensorML(DescribedObjectType sensorInfo)
        {
            return sensorInfo;
        }



        #endregion
    }
}
