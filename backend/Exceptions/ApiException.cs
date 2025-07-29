using System;
using System.Net;

namespace backend.Exceptions
{
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public override string Message { get; }
        public DateTime Timestamp { get; }
        public string? Details { get; }

        public ApiException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
            Message = message;
            Timestamp = DateTime.UtcNow;
        }

        public ApiException(HttpStatusCode statusCode, string message, string? details) : base(message)
        {
            StatusCode = statusCode;
            Message = message;
            Timestamp = DateTime.UtcNow;
            Details = details;
        }
    }
}