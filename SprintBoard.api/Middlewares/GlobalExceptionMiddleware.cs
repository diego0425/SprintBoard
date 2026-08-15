using System.Text.Json;
using SprintBoard.api.Errors;
using SprintBoard.Application.Exceptions;

namespace SprintBoard.api.Middlewares;

/// <summary>
/// Converts unhandled application exceptions into standardized HTTP error responses.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="next">
    /// Next middleware delegate in the HTTP request processing pipeline.
    /// </param>
    /// <param name="logger">
    /// Logger used to record unhandled exceptions intercepted by this middleware.
    /// </param>
    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Processes the current HTTP request and translates unhandled exceptions into JSON error responses.
    /// </summary>
    /// <param name="httpContext">
    /// Current HTTP context associated with the request being processed.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous execution of the middleware.
    /// </returns>
    public async Task Invoke(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. TraceId: {TraceId}",
                httpContext.TraceIdentifier);

            var (statusCode, message) = MapException(exception);

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = statusCode;

            var errorResponse = new ApiErrorResponse
            {
                StatusCode = statusCode,
                Message = message,
                TraceId = httpContext.TraceIdentifier
            };

            var serializedError = JsonSerializer.Serialize(errorResponse);
            await httpContext.Response.WriteAsync(serializedError);
        }
    }

    /// <summary>
    /// Maps an exception type to the HTTP status code and message that should be returned by the API.
    /// </summary>
    /// <param name="exception">
    /// Exception captured during request processing.
    /// </param>
    /// <returns>
    /// A tuple containing the HTTP status code and the corresponding response message.
    /// </returns>
    private static (int StatusCode, string Message) MapException(Exception exception)
    {
        return exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            InvalidOperationException => (StatusCodes.Status409Conflict, exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, exception.Message),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };
    }
}
