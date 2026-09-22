using System.Text.Json;
using SprintBoard.api.Errors;
using SprintBoard.Application.Exceptions;

namespace SprintBoard.api.Middlewares;

/// <summary>
/// Converts unhandled application exceptions into standardized
/// HTTP error responses and records structured diagnostic logs.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    private readonly ILogger<GlobalExceptionMiddleware>
        _logger;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="GlobalExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="next">
    /// Next middleware delegate in the HTTP request pipeline.
    /// </param>
    /// <param name="logger">
    /// Logger used to record exceptions intercepted by
    /// the middleware.
    /// </param>
    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Executes the next middleware and translates exceptions
    /// into standardized HTTP responses.
    /// </summary>
    /// <param name="httpContext">
    /// Current HTTP request context.
    /// </param>
    public async Task Invoke(
        HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception exception)
        {
            var (statusCode, message) =
                MapException(exception);

            LogException(
                httpContext,
                exception,
                statusCode);

            httpContext.Response.ContentType =
                "application/json";

            httpContext.Response.StatusCode =
                statusCode;

            var errorResponse =
                new ApiErrorResponse
                {
                    StatusCode =
                        statusCode,

                    Message =
                        message,

                    TraceId =
                        httpContext.TraceIdentifier
                };

            var serializedError =
                JsonSerializer.Serialize(
                    errorResponse);

            await httpContext.Response
                .WriteAsync(
                    serializedError);
        }
    }

    /// <summary>
    /// Records a warning for expected request failures and
    /// an error with stack trace for unexpected failures.
    /// </summary>
    /// <param name="httpContext">
    /// Current HTTP request context.
    /// </param>
    /// <param name="exception">
    /// Exception intercepted by the middleware.
    /// </param>
    /// <param name="statusCode">
    /// HTTP status code mapped from the exception.
    /// </param>
    private void LogException(
        HttpContext httpContext,
        Exception exception,
        int statusCode)
    {
        if (statusCode >=
            StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled request exception. " +
                "HTTP {RequestMethod} {RequestPath} " +
                "returned {StatusCode}. " +
                "CorrelationId: {CorrelationId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                statusCode,
                httpContext.TraceIdentifier);

            return;
        }

        _logger.LogWarning(
            "Handled request exception {ExceptionType}. " +
            "HTTP {RequestMethod} {RequestPath} " +
            "returned {StatusCode}. " +
            "CorrelationId: {CorrelationId}",
            exception.GetType().Name,
            httpContext.Request.Method,
            httpContext.Request.Path,
            statusCode,
            httpContext.TraceIdentifier);
    }

    /// <summary>
    /// Maps an exception type to the HTTP status code and
    /// message returned to the API client.
    /// </summary>
    /// <param name="exception">
    /// Exception captured during request processing.
    /// </param>
    /// <returns>
    /// Status code and public response message.
    /// </returns>
    private static (
        int StatusCode,
        string Message)
        MapException(
            Exception exception)
    {
        return exception switch
        {
            ArgumentException =>
                (
                    StatusCodes.Status400BadRequest,
                    exception.Message
                ),

            KeyNotFoundException =>
                (
                    StatusCodes.Status404NotFound,
                    exception.Message
                ),

            InvalidOperationException =>
                (
                    StatusCodes.Status409Conflict,
                    exception.Message
                ),

            UnauthorizedAccessException =>
                (
                    StatusCodes.Status401Unauthorized,
                    exception.Message
                ),

            ForbiddenAccessException =>
                (
                    StatusCodes.Status403Forbidden,
                    exception.Message
                ),

            _ =>
                (
                    StatusCodes
                        .Status500InternalServerError,
                    "An unexpected error occurred."
                )
        };
    }
}