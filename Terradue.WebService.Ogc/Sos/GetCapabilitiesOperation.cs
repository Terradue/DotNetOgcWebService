using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml.Serialization;
using Terradue.ServiceModel.Ogc;
using Terradue.ServiceModel.Ogc.Exceptions;
using Terradue.ServiceModel.Ogc.Ows11;
using Terradue.ServiceModel.Ogc.Sos20;
using Terradue.ServiceModel.Ogc.Swes20;
using Terradue.WebService.Ogc.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Configuration;

namespace Terradue.WebService.Ogc.Sos {
    /// <summary>
    /// Represents a sample GetCapabilities request handler
    /// </summary>
    public class GetCapabilitiesOperation : BaseSosOperation
    {


        /// <summary>
        /// Object used for thread synchronization
        /// </summary>
        private static object _lock = new object();

        /// <summary>
        /// Instace of <see cref="ServiceProvider"/>.
        /// </summary>
        private static ServiceProvider _serviceProvider;

        /// <summary>
        /// Instance of <see cref="ServiceIdentification"/>.
        /// </summary>
        private static ServiceIdentification _serviceIdentification;

        /// <summary>
        /// Specifies list of supported formats.
        /// </summary>
        private List<OutputFormat> _supportedFormats = new List<OutputFormat>()
        {
            OutputFormat.TextXml,
            OutputFormat.ApplicationXml,
        };

        private ReadOnlyCollection<string> _supportedVersions;
        /// <summary>
        /// Gets list of supported versions
        /// </summary>
        protected virtual ReadOnlyCollection<string> SupportedVersions
        {
            get
            {
                if (this._supportedVersions == null)
                {
                    this._supportedVersions = (from a in base.Services.ToList()
                                               where
                                                   a.Service == this.ServiceName
                                                   && a.Version.ToVersionNumber() > -1
                                               select a.Version).Distinct().ToList().AsReadOnly();
                }

                return this._supportedVersions;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetCapabilitiesOperation"/> class.
        /// </summary>
        /// <param name="configuration">Operation configuration.</param>
        public GetCapabilitiesOperation(ServiceOperationElement configuration, SosEntitiesFactory entitiesFactory, IHttpContextAccessor accessor, IMemoryCache cache)
            : base(configuration, entitiesFactory, accessor, cache)
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
                    Name = "updateSequence",
                    AnyValue = new AnyValue(),
                });

                parameters.Add(new DomainType()
                {
                    Name = "AcceptFormats",
                    AllowedValues = new Collection<object>(),
                });

                if (this._supportedFormats.Count > 0)
                {
                    DomainType parameter = parameters[parameters.Count - 1];

                    foreach (var format in this._supportedFormats)
                    {
                        parameter.AllowedValues.Add(new Terradue.ServiceModel.Ogc.Ows11.ValueType(format.ToStringValue()));
                    }
                }

                parameters.Add(new DomainType()
                {
                    Name = "AcceptVersions",
                    AllowedValues = new Collection<object>(),
                });

                if (this.SupportedVersions.Count > 0)
                {
                    DomainType parameter = parameters[parameters.Count - 1];

                    foreach (var version in this.SupportedVersions)
                    {
                        parameter.AllowedValues.Add(new Terradue.ServiceModel.Ogc.Ows11.ValueType(version));
                    }
                }

                parameters.Add(new DomainType()
                {
                    Name = "Sections",
                    AllowedValues = new Collection<object>()
                    {
                        new Terradue.ServiceModel.Ogc.Ows11.ValueType("ServiceIdentification"),
                        new Terradue.ServiceModel.Ogc.Ows11.ValueType("ServiceProvider"),
                        new Terradue.ServiceModel.Ogc.Ows11.ValueType("OperationsMetadata"),
                        new Terradue.ServiceModel.Ogc.Ows11.ValueType("Contents"),
                        new Terradue.ServiceModel.Ogc.Ows11.ValueType("Filter_Capabilities"),
                        new Terradue.ServiceModel.Ogc.Ows11.ValueType("All"),
                    },
                });

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
                    Href = string.Format(CultureInfo.InvariantCulture, "{0}/?service={1}&request={2}", this.ServiceBaseUri, this.ServiceName, this.RequestName)
                });

                dcp.Http.GetMethods.Add(new RequestMethodType()
                {
                    Href = string.Format(CultureInfo.InvariantCulture, "{0}/?service={1}&request={2}", this.ServiceBaseUri, this.ServiceName, this.RequestName)
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
            get { return typeof(GetCapabilities); }
        }

        /// <summary>
        /// Process operation request
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Resposne object to be sent back to the client</returns>
        public override OperationResult ProcessRequest(HttpRequest request, OwsRequestBase payload = null)
        {
            GetCapabilities getCapabilities = payload as GetCapabilities;

            //  Validate request parameter
            if (getCapabilities == null)
            {
                throw new NoApplicableCodeException(string.Format(CultureInfo.CurrentCulture, "Type '{0}' is invalid request type.", request.GetType().FullName));
            }

            OperationResult result = new OperationResult(request);

            //  TODO:   Filter_Capabilities is not yet implemented
            //  TODO:   Consider to implement updateSequence as described in OGC 06-121r3 7.3.4

            //  Validate acceptFormats parameter
            if (getCapabilities.AcceptFormats != null)
            {
                var formats = from sf in this._supportedFormats
                              from af in getCapabilities.AcceptFormats
                              where sf.ToStringValue() == af.Trim()
                              select sf;
                if (formats.Count() > 0)
                {
                    result.OutputFormat = formats.First();
                }
            }

            Capabilities capabilities = new Capabilities();

            //  Make sure client can accept current version of response
            if (getCapabilities.AcceptVersions != null && !getCapabilities.AcceptVersions.Contains(capabilities.Version))
            {
                throw new VersionNegotiationException(string.Format(CultureInfo.InvariantCulture, "Only '{0}' version is supported", capabilities.Version));
            }

            //  Make sure client can accept current formats
            if (getCapabilities.AcceptFormats != null)
            {
                bool supportedFormatFound = false;
                foreach (var format in getCapabilities.AcceptFormats)
                {
                    OutputFormat outputFormat;
                    supportedFormatFound = format.TryParseEnum<OutputFormat>(out outputFormat);
                    if (supportedFormatFound)
                    {
                        break;
                    }
                }
                if (!supportedFormatFound)
                {
                    throw new InvalidParameterValueException("Provided accept formats are not supported");
                }
            }

            //  TODO:   updateSequence currently not implemented
            //capabilities.UpdateSequence = DateTime.UtcNow.ToUnicodeStringValue();

            if (getCapabilities.Sections == null || getCapabilities.Sections.Contains("ServiceProvider"))
            {
                capabilities.ServiceProvider = this.GetServiceProvider();
            }

            if (getCapabilities.Sections == null || getCapabilities.Sections.Contains("ServiceIdentification"))
            {
                capabilities.ServiceIdentification = this.GetServiceIdentification();
            }

            if (getCapabilities.Sections == null || getCapabilities.Sections.Contains("OperationsMetadata"))
            {
                capabilities.OperationsMetadata = this.GetOperationsMetadata();
            }

            if (getCapabilities.Sections == null || getCapabilities.Sections.Contains("Filter_Capabilities"))
            {
                capabilities.FilterCapabilities = this.GetFilterCapabilities();
            }

            if (getCapabilities.Sections == null || getCapabilities.Sections.Contains("Contents"))
            {
                capabilities.Contents = this.GetContents();
            }

            result.ResultObject = capabilities;

            return result;
        }

        #endregion

        /// <summary>
        /// Gets ServiceProvider section
        /// </summary>
        /// <returns></returns>
        protected virtual ServiceProvider GetServiceProvider()
        {
            if (_serviceProvider == null)
            {
                //  Get ServiceProvider from XML file
                XmlSerializer serializer = new XmlSerializer(typeof(ServiceProvider));
                var filePath = ConfigurationManager.AppSettings["filepath_GetCapabilities_ServiceProvider"];
                ServiceProvider sp = (ServiceProvider)serializer.Deserialize(File.OpenText(filePath));

                lock (_lock)
                {
                    if (_serviceProvider == null)
                    {
                        _serviceProvider = sp;
                    }
                }
            }

            return _serviceProvider;
        }

        /// <summary>
        /// Gets ServiceIdentification section
        /// </summary>
        /// <returns></returns>
        protected virtual ServiceIdentification GetServiceIdentification()
        {
            if (_serviceIdentification == null)
            {
                //  Get ServiceProvider from XML file
                XmlSerializer serializer = new XmlSerializer(typeof(ServiceIdentification));
                //var uriBuilder = new UriBuilder {
                //    Host = this.HttpContext.Request.Host.Host,
                //    Scheme = this.HttpContext.Request.Scheme,
                //    Path = imageVirtualPath
                //};
                
                //if (request.Host.Port.HasValue)
                //    uriBuilder.Port = request.Host.Port.Value;
                //var url = uriBuilder.ToString();
                var filePath = ConfigurationManager.AppSettings["filepath_GetCapabilities_ServiceIdentification"];
                ServiceIdentification si = (ServiceIdentification)serializer.Deserialize(File.OpenText(filePath));

                lock (_lock)
                {
                    if (_serviceIdentification == null)
                    {
                        _serviceIdentification = si;
                    }
                }
            }

            return _serviceIdentification;
        }

        /// <summary>
        /// Gets OperationsMetadata section
        /// </summary>
        /// <returns></returns>
        protected virtual OperationsMetadata GetOperationsMetadata()
        {
            OperationsMetadata om = new OperationsMetadata();

            //  Add services information base on configuration
            foreach (ServiceOperationElement operationInfo in this.Services.ToList())
            {
                //  Take only SOS services
                if (!operationInfo.Service.Equals(this.ServiceName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                BaseOperation operationHandler = operationInfo.CreateHandlerInstance(this.Accessor, this.Cache);

                Operation operation = new Operation();

                operation.Name = operationInfo.Operation;

                //  Add operation dcps
                if (operationHandler.OperationDcps != null)
                {
                    foreach (Dcp dcp in operationHandler.OperationDcps)
                    {
                        operation.Dcps.Add(dcp);
                    }
                }

                //  Add operation parameters
                if (operationHandler.OperationParameters != null)
                {
                    foreach (DomainType parameter in operationHandler.OperationParameters)
                    {
                        operation.Parameters.Add(parameter);
                    }
                }

                //  Add operation constrains
                if (operationHandler.OperationConstraints != null)
                {
                    foreach (DomainType constraint in operationHandler.OperationConstraints)
                    {
                        operation.Constraints.Add(constraint);
                    }
                }

                //  Add operation metadata
                if (operationHandler.OperationMetadata != null)
                {
                    foreach (MetadataType metadata in operationHandler.OperationMetadata)
                    {
                        operation.Metadata.Add(metadata);
                    }
                }

                om.Operations.Add(operation);
            }

            //  Specify service parameter
            om.Parameters.Add(new DomainType()
            {
                Name = "service",
                AllowedValues = new Collection<object>()
                {
                    new Terradue.ServiceModel.Ogc.Ows11.ValueType(this.ServiceName),
                },
            });

            om.Parameters.Add(new DomainType()
            {
                Name = "version",
                AllowedValues = new Collection<object>(),
            });

            if (this.SupportedVersions.Count > 0)
            {
                DomainType parameter = om.Parameters[om.Parameters.Count - 1];

                foreach (var version in this.SupportedVersions)
                {
                    parameter.AllowedValues.Add(new Terradue.ServiceModel.Ogc.Ows11.ValueType(version));
                }
            }

            return om;
        }

        /// <summary>
        /// Gets Filter_Capabilities section
        /// </summary>
        /// <returns></returns>
        protected virtual FilterCapabilities GetFilterCapabilities()
        {
            return null;
        }

        /// <summary>
        /// Gets Contents section
        /// </summary>
        /// <returns></returns>
        protected virtual CapabilitiesContents GetContents()
        {
            CapabilitiesContents c = new CapabilitiesContents();
            c.Contents = new ContentsType();

            var offerings = this.SosEntitiesFactory.GetOfferings();

            c.Contents.offering = offerings.Values.Select(o => new AbstractContentsTypeOffering() { AbstractOffering = o}).ToArray();

            return c;
        }




    }
}
