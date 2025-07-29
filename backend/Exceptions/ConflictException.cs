using System.Net;

namespace backend.Exceptions
{
    public class ConflictException : ApiException
    {
        public ConflictException(string message)
            : base(HttpStatusCode.Conflict, message) { }

        public ConflictException(string message, string details)
            : base(HttpStatusCode.Conflict, message, details) { }

        public ConflictException(string resource, string operation, string reason)
            : base(HttpStatusCode.Conflict, $"Cannot {operation} {resource}: {reason}") { }
    }
}