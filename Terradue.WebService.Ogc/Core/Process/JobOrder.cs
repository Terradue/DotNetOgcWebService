using System;
using Terradue.ServiceModel.Ogc.Wps10;

namespace Terradue.WebService.Ogc.Core
{
	public class JobOrder
	{
        public DateTime CreationTime { get; set; }
        public Execute ExecuteRequest { get; set; }

		public JobProgress JobProgress { get; set; }
	}
}