using System.Net;

namespace backend.Exceptions
{
    public class ServiceUnavailableException : ApiException
    {
        public ServiceUnavailableException(string message)
            : base(HttpStatusCode.ServiceUnavailable, message) { }

        public ServiceUnavailableException(string message, string details)
            : base(HttpStatusCode.ServiceUnavailable, message, details) { }

        public ServiceUnavailableException(string serviceName, Exception cause)
            : base(HttpStatusCode.ServiceUnavailable, $"Service {serviceName} is currently unavailable: {cause.Message}") { }
    }
}