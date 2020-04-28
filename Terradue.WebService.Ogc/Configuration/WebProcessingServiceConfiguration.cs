using System.Configuration;

namespace Terradue.WebService.Ogc.Configuration {
    /// <summary>
    /// Represents a SweServiceRequest element within a configuration file.
    /// </summary>
    public class WebProcessingServiceConfiguration : ConfigurationSection
    {

        private static WebProcessingServiceConfiguration _settings;
        /// <summary>
        /// Gets instance of ServiceConfiguration.
        /// </summary>
        public static WebProcessingServiceConfiguration Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = ConfigurationManager.GetSection("WebProcessingService") as WebProcessingServiceConfiguration;
                }

                return _settings;
            }
        }

        /// <summary>
        /// Gets collection of processes.
        /// </summary>
        [ConfigurationProperty("processes", IsDefaultCollection = true)]
        public ConfigurationCollection<ProcessElement> Processes
        {
            get
            {
                var processes = (ConfigurationCollection<ProcessElement>)base["processes"];
                //processes.ServiceConfiguration = this;
                return processes;
            }
        }



        /// <summary>
        /// Gets or sets connection string to be used by this operation handler
        /// </summary>
        [ConfigurationProperty("jobCacheTimeout", IsRequired = false, DefaultValue = 24)]
        public int JobCacheTimeout
        {
            get
            {
                return (int)this["jobCacheTimeout"];
            }
            set
            {
                this["jobCacheTimeout"] = value;
            }
        }

        /// <summary>
        /// Gets or sets Base Url
        /// </summary>
        /// <example>https://store.terradue.com/sapi</example>
        [ConfigurationProperty("jobStatusBaseUrl", DefaultValue = "http://127.0.0.1:8080/t2api/wpsJobStatus")]
        [RegexStringValidator(@"https?://[\w\-\.~#\$&\+\/:=\?%]+")]
        public string JobStatusBaseUrl
        {
            get
            {
                return (string)this["jobStatusBaseUrl"];
            }

            set
            {
                this["jobStatusBaseUrl"] = value;
            }
        }
    }
}
