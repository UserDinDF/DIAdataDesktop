using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DIAdataDesktop.Models
{
    public sealed class DiaApiException : Exception
    {
        public string RequestUrl { get; }
        public HttpStatusCode StatusCode { get; }
        public string ResponseBody { get; }
        public long ElapsedMs { get; }

        public DiaApiException(string message, string requestUrl, HttpStatusCode statusCode, string responseBody, long elapsedMs, Exception? inner = null)
            : base($"{message}\nURL: {requestUrl}\nStatus: {(int)statusCode} {statusCode}\nElapsed: {elapsedMs}ms\nBody: {Trim(responseBody, 1200)}", inner)
        {
            RequestUrl = requestUrl;
            StatusCode = statusCode;
            ResponseBody = responseBody;
            ElapsedMs = elapsedMs;
        }

        private static string Trim(string s, int max) => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max) + "…");
    }
}
