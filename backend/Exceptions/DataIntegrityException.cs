using System.Net;

namespace backend.Exceptions
{
    public class DataIntegrityException : ApiException
    {
        public DataIntegrityException(string message)
            : base(HttpStatusCode.Conflict, message) { }

        public DataIntegrityException(string message, string details)
            : base(HttpStatusCode.Conflict, message, details) { }

        public DataIntegrityException(string entity, string operation, string reason)
            : base(HttpStatusCode.Conflict, $"Cannot {operation} {entity}: {reason}") { }
    }
}