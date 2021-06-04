using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Terradue.ServiceModel.Ogc.Wps10;
using Terradue.WebService.Ogc.Core;

namespace Terradue.WebService.Ogc.Wps {
	public class WpsProcess : IProcess {
		AsyncWPSProcess iprocess;

		public WpsProcess(AsyncWPSProcess iprocess) {
			this.iprocess = iprocess;
		}

		public void SetMemoryCache(IMemoryCache cache) {
			this.iprocess.Cache = cache;
		}

        public IMemoryCache GetMemoryCache() {
			return this.iprocess.Cache;
        }

        public void SetLogger(ILogger logger) {
			this.iprocess.Logger = logger;
        }

		public ILogger GetLogger() {
			return this.iprocess.Logger;
		}

		public TimeSpan JobCacheTime { get; internal set; }

		public ProcessDescriptionType ProcessDescription
		{
			get
			{
				return iprocess.ProcessDescription;
			}
		}

		public ProcessBriefType ProcessBrief
		{
			get
			{
				return iprocess.ProcessBrief;
			}
		}

		public string Id
		{
			get
			{
				return iprocess.Id;
			}
		}

		public string Version
        {
			get;
        }
        
        public Task<ExecuteResponse> CreateTask(JobOrder order)
		{
			return iprocess.CreateTask(order);
		}



		public ExecuteResponse SubmitExecuteProcess(Execute execute)
		{
			WpsJob job = new WpsJob(this, execute);

			return job.Run();
		}

        internal ExecuteResponse GetExecuteResponse() {
			return iprocess.GetExecuteResponse();

		}

		internal List<string> GetReport() {
			return iprocess.GetReport();

		}
	}
}