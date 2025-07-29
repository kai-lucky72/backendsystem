using System.Net;
using System.Text.Json;
using backend.DTOs.Error;

namespace backend.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IConfiguration _config;
    private static readonly string[] ExcludedPaths = new[]
    {
        "/api/auth/login", "/auth/login", "/swagger", "/api-docs", "/health"
    };
    private static readonly Dictionary<string, (int Count, DateTime WindowStart)> _counters = new();
    private static readonly object _lock = new();

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        bool enabled = _config.GetValue("RateLimit:Enabled", true);
        int maxRequests = _config.GetValue("RateLimit:Requests", 100);
        int windowSeconds = _config.GetValue("RateLimit:TimeWindowSeconds", 60);
        if (!enabled || ExcludedPaths.Any(p => context.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        string key = GetRateLimitKey(context);
        DateTime now = DateTime.UtcNow;
        int count;
        DateTime windowStart;
        lock (_lock)
        {
            if (_counters.TryGetValue(key, out var entry))
            {
                if ((now - entry.WindowStart).TotalSeconds > windowSeconds)
                {
                    // Reset window
                    count = 1;
                    windowStart = now;
                }
                else
                {
                    count = entry.Count + 1;
                    windowStart = entry.WindowStart;
                }
            }
            else
            {
                count = 1;
                windowStart = now;
            }
            _counters[key] = (count, windowStart);
        }

        if (count > maxRequests)
        {
            long retryAfter = windowSeconds - (long)(now - windowStart).TotalSeconds;
            var error = new ApiErrorResponse
            {
                Status = (int)HttpStatusCode.TooManyRequests,
                Message = "Rate limit exceeded. Please try again later.",
                Timestamp = DateTime.UtcNow,
                Details = $"Too many requests. Try again after {retryAfter} seconds."
            };
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            context.Response.Headers["Retry-After"] = retryAfter.ToString();
            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
            return;
        }

        await _next(context);
    }

    private string GetRateLimitKey(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
            return "user:" + context.User.Identity.Name;
        return "ip:" + context.Connection.RemoteIpAddress?.ToString();
    }
}