using System.Net;

namespace backend.Exceptions
{
    public class InvalidInputException : ApiException
    {
        public InvalidInputException(string message)
            : base(HttpStatusCode.BadRequest, message) { }

        public InvalidInputException(string message, string details)
            : base(HttpStatusCode.BadRequest, message, details) { }
    }
}