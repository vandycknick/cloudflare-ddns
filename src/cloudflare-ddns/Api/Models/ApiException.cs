using System;
using System.Net;

namespace CloudflareDDNS.Api.Models
{
    public class ApiException : Exception
    {
        public ApiException(int code, string message, HttpStatusCode statusCode) : base(message)
        {
            Code = code;
            StatusCode = statusCode;
        }

        public int Code { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }

}
