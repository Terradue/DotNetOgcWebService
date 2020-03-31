using System;
using System.Linq;
using System.Collections.Generic;
using Terradue.Core.Process;
using Terradue.ServiceModel.Ogc.Wps10;
using System.Threading;
using System.Threading.Tasks;

namespace Terradue.WebService.Ogc.Wps
{
	public class WpsProcess : IProcess
	{
		AsyncWPSProcess iprocess;

		public WpsProcess(AsyncWPSProcess iprocess)
		{
			this.iprocess = iprocess;

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


	}
}