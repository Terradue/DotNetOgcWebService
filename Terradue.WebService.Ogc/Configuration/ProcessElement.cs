using System;
using System.Configuration;
using System.Globalization;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Terradue.WebService.Ogc.Core;
using Terradue.WebService.Ogc.Wps;

namespace Terradue.WebService.Ogc.Configuration
{
    /// <summary>
    /// Represents a SweServiceRequest element within a configuration file.
    /// </summary>
    public class ProcessElement : ConfigurationElement, IConfigurationElement
    {


        private WpsProcess process;

        /// <summary>
        /// Holds a reference to default handler type
        /// </summary>
        private Type _handlerType;

        #region IConfigurationElement Members

        /// <summary>
        /// Gets or sets service configuration name
        /// </summary>
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }

        /// <summary>
        /// Gets or sets service configuration settings
        /// </summary>
        /// <value>The service configuration settings.</value>
        public ServiceConfiguration ServiceConfiguration { get; set; }

        #endregion

        /// <summary>
        /// Gets or sets service name. Must be three character service name.
        /// </summary>
        /// <example>SOS, SAS etc'</example>
        [ConfigurationProperty("identifier", IsRequired = true)]
        [RegexStringValidator(@"^([a-zA-Z0-9]+)?$")]
        public string Identifier
        {
            get
            {
                return (string)this["identifier"];
            }

            set
            {
                this["identifier"] = value;
            }
        }

        /// <summary>
        /// Gets or sets process abstract
        /// </summary>
        /// <example>GetCapabilities</example>
        [ConfigurationProperty("abstract", IsRequired = true)]
        public string Abstract
        {
            get
            {
                return (string)this["abstract"];
            }

            set
            {
                this["abstract"] = value;
            }
        }

        /// <summary>
        /// Gets or sets process title
        /// </summary>
        /// <example>GetCapabilities</example>
        [ConfigurationProperty("title", IsRequired = true)]
        public string Title
        {
            get
            {
                return (string)this["title"];
            }

            set
            {
                this["title"] = value;
            }
        }

        /// <summary>
        /// Gets or sets process version
        /// </summary>
        /// <example>GetCapabilities</example>
        [ConfigurationProperty("version", IsRequired = true)]
        public string Version {
            get {
                return (string)this["version"];
            }

            set {
                this["version"] = value;
            }
        }

        /// <summary>
        /// Gets or sets operation handler type
        /// </summary>
        [ConfigurationProperty("type", IsRequired = true)]
        public string DefaultHandlerType
        {
            get
            {
                return (string)this["type"];
            }

            set
            {
                this["type"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether cache is enabled or not.
        /// </summary>
        [ConfigurationProperty("async", IsRequired = false, DefaultValue = false)]
        public bool Async
        {
            get
            {
                return (bool)this["async"];
            }
            set
            {
                this["async"] = value;
            }
        }

		/// <summary>
		/// Gets or sets a value indicating whether cache is enabled or not.
		/// </summary>
		[ConfigurationProperty("jobCachePeriod", IsRequired = false, DefaultValue = 36000)]
		public int JobCachePeriod
		{
			get
			{
				return (int)this["jobCachePeriod"];
			}
			set
			{
				this["jobCachePeriod"] = value;
			}
		}

        /// <summary>
        /// Creates an instance of an operation handler
        /// </summary>
        /// <returns>An operation handler instance</returns>
        public WpsProcess CreateHandlerInstance(IHttpContextAccessor accessor, IMemoryCache cache, HttpClient httpClient, ILogger logger, bool forceCreateNewProcess = false)
        {
            if (this.process == null || forceCreateNewProcess)
            {
                if (this._handlerType == null)
                {
                    this._handlerType = Type.GetType(this.DefaultHandlerType);
                }
                if (this._handlerType == null)
                {
                    throw new ConfigurationErrorsException(string.Format(CultureInfo.InvariantCulture, "Type '{0}' is not found.", this.DefaultHandlerType));
                }

				var iprocess = Activator.CreateInstance(this._handlerType, this.Identifier, this.Title, this.Abstract, this.Version) as AsyncWPSProcess;
                this.process = new WpsProcess(iprocess);
				this.process.JobCacheTime = TimeSpan.FromSeconds(this.JobCachePeriod);
                this.process.SetHttpClient(httpClient);
                this.process.SetMemoryCache(cache);
                this.process.SetLogger(logger);
            }

            return this.process;
        }
    }
}
