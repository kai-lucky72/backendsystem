using System.Net;

namespace backend.Exceptions
{
    public class TimeframeViolationException : ApiException
    {
        public TimeframeViolationException(TimeSpan startTime, TimeSpan endTime)
            : base(HttpStatusCode.Forbidden, $"Operation can only be performed between {startTime} and {endTime}") { }

        public TimeframeViolationException(string message)
            : base(HttpStatusCode.Forbidden, message) { }
    }
}