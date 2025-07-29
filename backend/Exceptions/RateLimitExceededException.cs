using System.Net;

namespace backend.Exceptions
{
    public class RateLimitExceededException : ApiException
    {
        public long RetryAfterSeconds { get; }

        public RateLimitExceededException(string message, long retryAfterSeconds)
            : base(HttpStatusCode.TooManyRequests, message)
        {
            RetryAfterSeconds = retryAfterSeconds;
        }

        public RateLimitExceededException(string message, string details, long retryAfterSeconds)
            : base(HttpStatusCode.TooManyRequests, message, details)
        {
            RetryAfterSeconds = retryAfterSeconds;
        }

        public RateLimitExceededException(long retryAfterSeconds)
            : base(HttpStatusCode.TooManyRequests, "Rate limit exceeded. Please try again later.")
        {
            RetryAfterSeconds = retryAfterSeconds;
        }
    }
}