using System.Net;

namespace backend.Exceptions
{
    public class UnprocessableEntityException : ApiException
    {
        public UnprocessableEntityException(string message)
            : base(HttpStatusCode.UnprocessableEntity, message) { }

        public UnprocessableEntityException(string message, string details)
            : base(HttpStatusCode.UnprocessableEntity, message, details) { }

        public UnprocessableEntityException(string fieldName, object fieldValue, string reason)
            : base(HttpStatusCode.UnprocessableEntity, $"Invalid value '{fieldValue}' for {fieldName}: {reason}") { }
    }
}