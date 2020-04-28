using System.Collections.Specialized;
using System.Web;
using System.Net.Http;
using Terradue.ServiceModel.Ogc.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace Terradue.WebService.Ogc.Swc {
    /// <summary>
    /// This class handles HTTP operations that can be used for OGC Service. 
    /// </summary>
    public class SwcStationHttpRequestHandler : HttpRequestHandler
    {
        public SwcStationHttpRequestHandler(IHttpContextAccessor accessor, IMemoryCache cache, HttpClient httpClient) : base(accessor, cache, httpClient) { }

        /// <summary>
        /// Proccesses HTTP request
        /// </summary>
        /// <param name="request">Message with request details</param>
        /// <returns>Response to the request.</returns>
        public IActionResult ProcessRequest(string stationId)
        {
            OperationResult result = null;

            if (this.HttpAccessor == null || this.HttpAccessor.HttpContext == null || this.HttpAccessor.HttpContext.Request == null)
                throw new Exception("Invalid Http context");

            var request = this.HttpAccessor.HttpContext.Request;

            try
            {
                NameValueCollection queryParameters = HttpUtility.ParseQueryString(request.QueryString.Value);

                string cacheKey = string.Empty;

                //  Create cache key to be used to store results in cache
                cacheKey = Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(request);

                //  Get cahce resuls if exists
                result = this.Cache.Get<OperationResult>(cacheKey);

                if (result == null)
                {
                    //  Hanle request and return results back
                    result = GetStation(request, stationId);
                }

            }
            catch (System.Exception exp)
            {
                //  Handle all other .NET errors
                result = new OperationResult(request)
                {
                    ResultObject = new NoApplicableCodeException("Application error.", exp).ExceptionReport,
                };
            }

            return result;
        }

        static OperationResult GetStation(HttpRequest messageRequest, string stationId)
        {
            OperationResult getStation = new OperationResult(messageRequest);

            return getStation;
        }
   }
}