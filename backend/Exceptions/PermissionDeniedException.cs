using System.Net;

namespace backend.Exceptions
{
    public class PermissionDeniedException : ApiException
    {
        public PermissionDeniedException(string message)
            : base(HttpStatusCode.Forbidden, message) { }

        public PermissionDeniedException(string message, string details)
            : base(HttpStatusCode.Forbidden, message, details) { }

        public PermissionDeniedException()
            : base(HttpStatusCode.Forbidden, "Permission denied") { }
    }
}