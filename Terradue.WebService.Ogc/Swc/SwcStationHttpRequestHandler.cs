using System.Collections.Specialized;
using System.Web;
using System.Net.Http;
using Terradue.ServiceModel.Ogc.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Terradue.WebService.Ogc.Swc {
    /// <summary>
    /// This class handles HTTP operations that can be used for OGC Service. 
    /// </summary>
    public class SwcStationHttpRequestHandler
    {

        /// <summary>
        /// Proccesses HTTP request
        /// </summary>
        /// <param name="messageRequest">Message with request details</param>
        /// <returns>Response to the request.</returns>
        public static IActionResult ProcessRequest(HttpRequestMessage messageRequest, string stationId, IMemoryCache cache)
        {
            OperationResult result = null;

            try
            {
                NameValueCollection queryParameters = HttpUtility.ParseQueryString(messageRequest.RequestUri.Query);

                string cacheKey = string.Empty;

                //  Create cache key to be used to store results in cache
                cacheKey = messageRequest.RequestUri.ToString();

                //  Get cahce resuls if exists
                result = cache.Get<OperationResult>(cacheKey);

                if (result == null)
                {
                    //  Hanle request and return results back
                    result = GetStation(messageRequest, stationId);
                }

            }
            catch (System.Exception exp)
            {
                //  Handle all other .NET errors
                result = new OperationResult(messageRequest)
                {
                    ResultObject = new NoApplicableCodeException("Application error.", exp).ExceptionReport,
                };
            }

            return result;
        }

        static OperationResult GetStation(HttpRequestMessage messageRequest, string stationId)
        {
            OperationResult getStation = new OperationResult(messageRequest);

            return getStation;
        }
   }
}