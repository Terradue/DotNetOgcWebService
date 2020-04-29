using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Terradue.WebService.Ogc {

    public abstract class HttpRequestHandler : IHttpRequestHandler {

        protected readonly IHttpContextAccessor HttpAccessor;
        protected readonly IMemoryCache Cache;
        protected readonly HttpClient HttpClient;
        protected readonly ILogger Logger;

        public HttpRequestHandler(IHttpContextAccessor accessor, IMemoryCache cache, HttpClient httpClient, ILogger logger) {
            this.HttpAccessor = accessor;
            this.Cache = cache;
            this.HttpClient = httpClient;
            this.Logger = logger;
        }

        public virtual OperationResult ProcessRequest() {
            throw new NotImplementedException();
        }

        public virtual Task<OperationResult> ProcessRequestAsync(CancellationToken token) {
            throw new NotImplementedException();
        }
    }

    public interface IHttpRequestHandler {

        OperationResult ProcessRequest();
        Task<OperationResult> ProcessRequestAsync(CancellationToken token);

    }
}
