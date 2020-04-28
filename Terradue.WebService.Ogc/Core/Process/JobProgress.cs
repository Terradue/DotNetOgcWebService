using System;
using Terradue.ServiceModel.Ogc.Wps10;

namespace Terradue.WebService.Ogc.Core
{
	public class JobProgress : IProgress<Terradue.ServiceModel.Ogc.Wps10.StatusType>
	{
		public StatusType Status { get; set; }

		public void Report(StatusType value)
		{
			Status = value;
		}
	}
}