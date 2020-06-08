using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
        ILogger logger;

        IMemoryCache wpsJobCache;

        public WpsJob() { }
        public WpsJob(WpsProcess wpsProcess, Execute execute)
        {
            logger = wpsProcess.GetLogger();
            logger.LogInformation("Create new wpsjob");
            logger.LogDebug("Create new wpsjob -- start");
            this.wpsProcess = wpsProcess;
            this.wpsJobCache = this.wpsProcess.GetMemoryCache();
            progress = new JobProgress();
            creationTime = DateTime.UtcNow;
            this.jobOrder = new JobOrder() { ExecuteRequest = execute, JobProgress = progress, CreationTime = creationTime };
            this.Task = wpsProcess.CreateTask(jobOrder);
            Uid = Save(DateTimeOffset.Now.Add(wpsProcess.JobCacheTime));
            this.jobOrder.Uid = this.Uid;
            JobOrder.WriteExecuteRequest(execute, this.Uid);
            logger.LogDebug("Create new wpsjob -- end");
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
                logger.LogDebug("Set job cache -- {0}", uid.ToString());
                wpsJobCache.Set<WpsJob>(uid.ToString(), this, absoluteExpiration.DateTime);
                if (this.jobOrder != null && this.jobOrder.Uid != null && this.jobOrder.Uid != uid.ToString()) logger.LogDebug("Wrong uid -> {0}", this.jobOrder.Uid);
            }
            
            return uid.ToString();
        }

        private static WpsJob Load(IHttpContextAccessor accessor, IMemoryCache cache, HttpClient httpClient, ILogger logger, string uid, bool useCache = true)
        {
            WpsJob job = null;
            if (useCache) {
                lock (cache) {
                    logger.LogDebug("Get job cache -- {0}", uid);
                    job = cache.Get(uid) as WpsJob;
                    if (job.jobOrder.Uid != uid) logger.LogDebug("Wrong uid -> {0}", job.jobOrder.Uid);
                }
            }
            if (job == null) {
                try {
                    //read from file
                    var execute = JobOrder.ReadExecuteRequest(uid);
                    var executeResponse = JobOrder.ReadExecuteResponse(uid);
                    var recoveryInfo = JobOrder.ReadRecoveryInfo(uid);
                    if (recoveryInfo == null) return null;
                    job = new WpsJob();
                    job.logger = logger;
                    job.Uid = uid;
                    job.progress = new JobProgress();
                    job.progress.Report(executeResponse.Status);
                    job.jobOrder = new JobOrder() { ExecuteRequest = execute, JobProgress = job.progress, CreationTime = executeResponse.Status.creationTime };
                    job.jobOrder.ExecuteResponse = executeResponse;
                    job.jobOrder.Uid = uid;
                    job.jobOrder.RecoveryInfo = recoveryInfo;

                    //create wps process
                    foreach (var processConfig in WebProcessingServiceConfiguration.Settings.Processes) {
                        if (processConfig.Identifier == recoveryInfo.wpsProcessIdentifier) {
                            job.wpsProcess = processConfig.CreateHandlerInstance(accessor, cache, httpClient, logger, true);                            
                        }
                    }

                    if(job.wpsProcess != null) {                        
                        job.Task = job.wpsProcess.CreateTask(job.jobOrder);
                    }

                    lock (cache) {
                        logger.LogDebug("Set job cache -- {0}", uid);
                        cache.Set<WpsJob>(uid, job, DateTimeOffset.Now.Add(job.wpsProcess.JobCacheTime).DateTime);
                        if (job.jobOrder.Uid != uid) logger.LogDebug("Wrong uid -> {0}", job.jobOrder.Uid);
                    }

                    return job;

                } catch (Exception) {
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
            logger.LogInformation("Run wpsjob");
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

            logger.LogInformation("Get wpsjob ExecuteResponse");

            try
            {
                response = wpsProcess.GetExecuteResponse();
            }
            catch (AggregateException ae)
            {
                logger.LogError(ae.Message);
                logger.LogDebug(ae.StackTrace);
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
                            logger.LogError("Error WPS job {0} : {1}", Uid, ex2.Message);
                            logger.LogDebug(ex2.StackTrace);
                            pst.ExceptionReport.Exceptions.Add(new ExceptionType() { ExceptionCode = ExceptionCode.NoApplicableCode, ExceptionText = ex2.Message.Contains("Object reference") ? ex2.StackTrace : ex2.Message });
                        }
                    }
                    logger.LogError("Error WPS job {0} : {1}", Uid, ex.Message);
                    logger.LogDebug(ex.StackTrace);
                    pst.ExceptionReport.Exceptions.Add(new ExceptionType()
                    {
                        ExceptionCode = ExceptionCode.NoApplicableCode,
                        ExceptionText = ex.Message.Contains("Object reference") ? ex.StackTrace : ex.Message
                    });
                }
            }
            
            if (string.IsNullOrEmpty(response.serviceInstance)) {
                var uri = new Uri(WebProcessingServiceConfiguration.Settings.JobStatusBaseUrl);
                response.serviceInstance = string.Format("{0}://{1}/", uri.Scheme, uri.Host);
            }
            if (string.IsNullOrEmpty(response.statusLocation)) {
                response.statusLocation = string.Format("{0}/{1}", WebProcessingServiceConfiguration.Settings.JobStatusBaseUrl, Uid);
            }
            if (response.Process == null) {
                response.Process = wpsProcess.ProcessBrief;
            }
            if (string.IsNullOrEmpty(response.service)) {
                response.service = "WPS";
            }
            if (string.IsNullOrEmpty(response.version)) {
                response.version = "1.0.0";
            }

            jobOrder.ExecuteResponse = response;

            logger.LogDebug("Return response of type {0}",response.Status.ItemElementName.ToString());
            logger.LogDebug(response.statusLocation);

            return response;
        }

        private RecoveryInfo GetRecoveryInfo() {
            return jobOrder.RecoveryInfo;
        }

        private List<string> GetReport() {
            if (this.wpsProcess != null) {
                return this.wpsProcess.GetReport();
            }
            return null;
        }

        public static ExecuteResponse GetCachedExecuteResponse(IHttpContextAccessor accessor, IMemoryCache cache, HttpClient httpclient, ILogger logger, string uid)
        {
            var job = WpsJob.Load(accessor, cache, httpclient, logger, uid);
            if (job == null) throw new EntryPointNotFoundException();            
            return job.GetExecuteResponse();
        }

        public static ExecuteResponse GetExecuteResponse(IHttpContextAccessor accessor, IMemoryCache cache, HttpClient httpclient, ILogger logger, string uid) {
            var job = WpsJob.Load(accessor, cache, httpclient, logger, uid, false);
            if (job == null) throw new EntryPointNotFoundException();            
            return job.GetExecuteResponse();
        }

        public static RecoveryInfo GetRecoveryInfo(IHttpContextAccessor accessor, IMemoryCache cache, HttpClient httpclient, ILogger logger, string uid) {
            var job = WpsJob.Load(accessor, cache, httpclient, logger, uid);
            if (job == null) {
                throw new EntryPointNotFoundException();
            }
            return job.GetRecoveryInfo();
        }

        public static void ForceRetry(IHttpContextAccessor accessor, IMemoryCache cache, HttpClient httpclient, ILogger logger, string uid, int retry) {
            var job = WpsJob.Load(accessor, cache, httpclient, logger, uid);
            if (job == null) {
                throw new EntryPointNotFoundException();
            }
            job.jobOrder.RecoveryInfo.retry = retry;
            job.jobOrder.SetRecoveryInfo(job.jobOrder.RecoveryInfo);
        }

        public static List<string> GetReport(IHttpContextAccessor accessor, IMemoryCache cache, HttpClient httpclient, ILogger logger, string uid) {
            var job = WpsJob.Load(accessor, cache, httpclient, logger, uid);
            if (job == null) {
                throw new EntryPointNotFoundException();
            }
            return job.GetReport();
        }
    }
}