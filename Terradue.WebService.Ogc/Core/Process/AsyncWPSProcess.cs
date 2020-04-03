using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Terradue.ServiceModel.Ogc.Ows11;
using Terradue.ServiceModel.Ogc.Wps10;

namespace Terradue.WebService.Ogc.Core
{
    public abstract class AsyncWPSProcess : IAsyncProcess
    {
        public AsyncWPSProcess(string identifier, string title, string descr, string version = "1.0")
        {
            if (!string.IsNullOrEmpty(identifier))
                this.Identifier = new CodeType() { Value = identifier };
            if (!string.IsNullOrEmpty(title))
                this.Title = new LanguageStringType() { Value = title };
            if (!string.IsNullOrEmpty(descr))
                this.Abstract = new LanguageStringType() { Value = descr };
            if (!string.IsNullOrEmpty(version))
                this.Version = version;

            Status = new StatusType()
            {
                Item = new ProcessAcceptedType() { Value = "Process accepted" },
                ItemElementName = ItemChoiceType.ProcessAccepted,
                creationTime = DateTime.Now
            };
        }

        public CodeType Identifier { get; private set; }
        public LanguageStringType Title { get; private set; }
        public LanguageStringType Abstract { get; private set; }
        public string Version { get; private set; }

        public string Description
        {
            get
            {
                if ( Abstract != null )
                return Abstract.Value;
                return null;
            }
        }

        public StatusType Status { get; private set; }


        public abstract ProcessDescriptionType ProcessDescription
        {
            get;
        }

        public abstract ProcessBriefType ProcessBrief
        {
            get;
        }

        public abstract string Id
        {
            get;
        }



        public abstract Task Task { get; }

        public abstract Task<ExecuteResponse> CreateTask(JobOrder order);
    }



}
