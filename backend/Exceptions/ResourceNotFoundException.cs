using System.Net;

namespace backend.Exceptions
{
    public class ResourceNotFoundException : ApiException
    {
        public ResourceNotFoundException(string resourceName, string fieldName, object fieldValue)
            : base(HttpStatusCode.NotFound, $"{resourceName} not found with {fieldName}: '{fieldValue}'") { }

        public ResourceNotFoundException(string message)
            : base(HttpStatusCode.NotFound, message) { }
    }
}