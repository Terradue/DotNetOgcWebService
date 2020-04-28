using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Terradue.WebService.Ogc {

    public abstract class HttpRequestHandler : IHttpRequestHandler {

        protected readonly IHttpContextAccessor HttpAccessor;
        protected readonly IMemoryCache Cache;
        protected readonly HttpClient HttpClient;

        public HttpRequestHandler(IHttpContextAccessor accessor, IMemoryCache cache, HttpClient httpClient) {
            this.HttpAccessor = accessor;
            this.Cache = cache;
            this.HttpClient = httpClient;
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
