using System.Net;
using System.Text.Json;
using backend.DTOs.Error;
using backend.Exceptions;

namespace backend.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        ApiErrorResponse errorResponse;
        int statusCode;
        if (exception is ApiException apiEx)
        {
            statusCode = (int)apiEx.StatusCode;
            errorResponse = new ApiErrorResponse
            {
                Status = statusCode,
                Message = apiEx.Message,
                Timestamp = DateTime.UtcNow,
                Details = apiEx.Details
            };
            if (apiEx is RateLimitExceededException rateLimitEx)
            {
                context.Response.Headers["Retry-After"] = rateLimitEx.RetryAfterSeconds.ToString();
            }
        }
        else if (exception is UnauthorizedAccessException)
        {
            statusCode = StatusCodes.Status401Unauthorized;
            errorResponse = new ApiErrorResponse
            {
                Status = statusCode,
                Message = "Unauthorized",
                Timestamp = DateTime.UtcNow,
                Details = exception.Message
            };
        }
        else if (exception is ArgumentException || exception is InvalidOperationException)
        {
            statusCode = StatusCodes.Status400BadRequest;
            errorResponse = new ApiErrorResponse
            {
                Status = statusCode,
                Message = exception.Message,
                Timestamp = DateTime.UtcNow
            };
        }
        else
        {
            statusCode = StatusCodes.Status500InternalServerError;
            errorResponse = new ApiErrorResponse
            {
                Status = statusCode,
                Message = "An unexpected error occurred",
                Timestamp = DateTime.UtcNow,
                Details = exception.Message
            };
        }
        context.Response.StatusCode = statusCode;
        var jsonResponse = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(jsonResponse);
    }
}