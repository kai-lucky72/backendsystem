using System.Net;

namespace backend.Exceptions
{
    public class DuplicateResourceException : ApiException
    {
        public DuplicateResourceException(string resourceName, string fieldName, object fieldValue)
            : base(HttpStatusCode.Conflict, $"{resourceName} already exists with {fieldName}: '{fieldValue}'") { }

        public DuplicateResourceException(string message)
            : base(HttpStatusCode.Conflict, message) { }

        public DuplicateResourceException(string message, string details)
            : base(HttpStatusCode.Conflict, message, details) { }
    }
}