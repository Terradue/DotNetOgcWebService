using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using log4net;
using Microsoft.Extensions.Caching.Memory;
using Terradue.ServiceModel.Ogc.Ows11;
using Terradue.ServiceModel.Ogc.Wps10;
using Terradue.WebService.Ogc.Configuration;
using Terradue.WebService.Ogc.Core;

namespace Terradue.WebService.Ogc.Wps {
    public class WpsJob : IJob<ExecuteResponse>
    {
        JobOrder jobOrder;
        JobProgress progress;
        string uid;
        DateTime creationTime;
        WpsProcess wpsProcess;

        private static readonly ILog log = LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        IMemoryCache wpsJobCache;

        public WpsJob() { }
        public WpsJob(WpsProcess wpsProcess, Execute execute)
        {
            this.wpsProcess = wpsProcess;
            this.wpsJobCache = this.wpsProcess.GetMemoryCache();
            progress = new JobProgress();
            creationTime = DateTime.UtcNow;
            this.jobOrder = new JobOrder() { ExecuteRequest = execute, JobProgress = progress, CreationTime = creationTime };
            this.Task = wpsProcess.CreateTask(jobOrder);
            Uid = Save(DateTimeOffset.Now.Add(wpsProcess.JobCacheTime));
            this.jobOrder.Uid = this.Uid;
            JobOrder.WriteExecuteRequest(execute, this.Uid);
        }
        
        public Task<ExecuteResponse> Task
        {
            get;

            protected set;
        }

        private string Save(DateTimeOffset absoluteExpiration)
        {
            Guid uid = Guid.NewGuid();
            WpsJob job = null;
            while (wpsJobCache.TryGetValue<WpsJob>(uid.ToString(),out job))
            {
                Thread.Sleep(100);
                uid = Guid.NewGuid();
            }
            lock (wpsJobCache)
            {
                wpsJobCache.Set<WpsJob>(uid.ToString(), this, absoluteExpiration.DateTime);
            }
            
            return uid.ToString();
        }

        private static WpsJob Load(IMemoryCache cache, HttpClient httpClient, string uid)
        {
            WpsJob job = null;
            lock (cache)
            {
                job = cache.Get(uid) as WpsJob;
            }
            if (job == null) {
                try {
                    //read from file
                    var execute = JobOrder.ReadExecuteRequest(uid);
                    var executeResponse = JobOrder.ReadExecuteResponse(uid);
                    var recoveryInfo = JobOrder.ReadRecoveryInfo(uid);
                    if (recoveryInfo == null) return null;
                    job = new WpsJob();

                    job.progress = new JobProgress();
                    job.progress.Report(executeResponse.Status);
                    job.jobOrder = new JobOrder() { ExecuteRequest = execute, JobProgress = job.progress, CreationTime = executeResponse.Status.creationTime };
                    job.jobOrder.ExecuteResponse = executeResponse;
                    job.jobOrder.Uid = uid;
                    job.jobOrder.RecoveryInfo = recoveryInfo;

                    //create wps process
                    foreach (var processConfig in WebProcessingServiceConfiguration.Settings.Processes) {
                        if (processConfig.Identifier == recoveryInfo.wpsProcessIdentifier) {
                            job.wpsProcess = processConfig.CreateHandlerInstance();                            
                        }
                    }

                    if(job.wpsProcess != null) {
                        job.wpsProcess.SetHttpClient(httpClient);
                        job.wpsProcess.SetMemoryCache(cache);
                        job.Task = job.wpsProcess.CreateTask(job.jobOrder);
                    }

                    lock (cache) {
                        cache.Set<WpsJob>(uid, job, DateTimeOffset.Now.Add(job.wpsProcess.JobCacheTime).DateTime);
                    }

                    return job;

                } catch(Exception e) {
                    return null;
                }
            }
            return job;
        }

        public string Uid
        {
            get
            {
                return uid;
            }

            protected set
            {
                uid = value;
            }
        }
        
        public ExecuteResponse Run()
        {

            Task.Start();
            progress.Report(new StatusType()
            {
                Item = new ProcessStartedType() { Value = "Task Started", percentCompleted = "0" },
                ItemElementName = ItemChoiceType.ProcessStarted,
                creationTime = this.creationTime
            });
            Thread.Sleep(2000);

            return GetExecuteResponse();

        }

        public ExecuteResponse GetExecuteResponse()
        {
            ExecuteResponse response = null;

            try
            {
                response = wpsProcess.GetExecuteResponse();

                //if (Task.IsCompleted)
                //{
                //    response = Task.Result;
                //}
                //if (response == null)
                //{
                //    response = new ExecuteResponse();
                //    response.Status = progress.Status;
                //}
            }
            catch (AggregateException ae)
            {
                response = new ExecuteResponse();
                var pst = new ProcessFailedType();
                pst.ExceptionReport = new ServiceModel.Ogc.Ows11.ExceptionReport();
                response.Status = new StatusType();
                response.Status.Item = pst;
                response.Status.ItemElementName = ItemChoiceType.ProcessFailed;
                foreach (var ex in ae.InnerExceptions)
                {
                    if (ex is AggregateException)
                    {
                        var ae2 = ex as AggregateException;
                        foreach (var ex2 in ae2.InnerExceptions)
                        {
                            log.ErrorFormat("Error WPS job {0} : {1}", Uid, ex2.Message);
                            log.Debug(ex2.StackTrace);
                            pst.ExceptionReport.Exceptions.Add(new ExceptionType() { ExceptionCode = ExceptionCode.NoApplicableCode, ExceptionText = ex2.Message.Contains("Object reference") ? ex2.StackTrace : ex2.Message });
                        }
                    }
                    log.ErrorFormat("Error WPS job {0} : {1}", Uid, ex.Message);
                    log.Debug(ex.StackTrace);
                    pst.ExceptionReport.Exceptions.Add(new ExceptionType()
                    {
                        ExceptionCode = ExceptionCode.NoApplicableCode,
                        ExceptionText = ex.Message.Contains("Object reference") ? ex.StackTrace : ex.Message
                    });
                }
            }
            response.statusLocation = string.Format("{0}/{1}", WebProcessingServiceConfiguration.Settings.JobStatusBaseUrl, Uid);
            response.Process = wpsProcess.ProcessBrief;
            response.service = "WPS";
            response.version = "1.0.0";
            response.serviceInstance = "http://localhost/test";

            jobOrder.ExecuteResponse = response;

            return response;
        }

        public static ExecuteResponse GetCachedExecuteResponse(IMemoryCache cache, HttpClient httpclient, string uid)
        {
            var job = WpsJob.Load(cache, httpclient, uid);
            if (job == null)
            {
                throw new EntryPointNotFoundException();
            }
            return job.GetExecuteResponse();

        }
    }
}