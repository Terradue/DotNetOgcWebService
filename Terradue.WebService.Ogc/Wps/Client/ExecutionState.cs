using System;
using Terradue.ServiceModel.Ogc.Wps10;

namespace Terradue.WebService.Ogc.Wps.Client
{
    public class ExecutionState
    {
        public ExecutionState(){
            PollingPeriod = TimeSpan.FromMinutes(1);
        }

        public StatusType Status { get; set; }
        public TimeSpan PollingPeriod { get; set; }

        public object ExtensionState { get; set; }
    }
}