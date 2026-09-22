using Serilog.Context;

namespace SprintBoard.api.Middlewares;

/// <summary>
/// Ensures that every HTTP request has a correlation identifier
/// that can be used to trace its execution across application logs.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    /// <summary>
    /// Header used to receive and return the request
    /// correlation identifier.
    /// </summary>
    public const string HeaderName =
        "X-Correlation-ID";

    private const int MaximumCorrelationIdLength =
        64;

    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    /// <param name="next">
    /// Next middleware delegate in the HTTP pipeline.
    /// </param>
    public CorrelationIdMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Resolves or creates a correlation identifier
    /// and makes it available throughout the request.
    /// </summary>
    /// <param name="httpContext">
    /// Current HTTP request context.
    /// </param>
    public async Task Invoke(
        HttpContext httpContext)
    {
        var correlationId =
            GetOrCreateCorrelationId(
                httpContext);

        httpContext.TraceIdentifier =
            correlationId;

        httpContext.Response.Headers[
            HeaderName] =
            correlationId;

        using (
            LogContext.PushProperty(
                "CorrelationId",
                correlationId))
        {
            await _next(httpContext);
        }
    }

    /// <summary>
    /// Returns a valid correlation identifier supplied
    /// by the client or generates a new identifier.
    /// </summary>
    /// <param name="httpContext">
    /// Current HTTP request context.
    /// </param>
    /// <returns>
    /// Correlation identifier for the request.
    /// </returns>
    private static string GetOrCreateCorrelationId(
        HttpContext httpContext)
    {
        if (httpContext.Request.Headers
                .TryGetValue(
                    HeaderName,
                    out var values))
        {
            var candidate =
                values.FirstOrDefault()?
                    .Trim();

            if (IsValidCorrelationId(
                candidate))
            {
                return candidate!;
            }
        }

        return Guid.NewGuid()
            .ToString("N");
    }

    /// <summary>
    /// Determines whether a correlation identifier
    /// is safe to propagate through logs and headers.
    /// </summary>
    /// <param name="correlationId">
    /// Identifier supplied by the client.
    /// </param>
    /// <returns>
    /// True when the identifier is valid.
    /// </returns>
    private static bool IsValidCorrelationId(
        string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(
                correlationId) ||
            correlationId.Length >
                MaximumCorrelationIdLength)
        {
            return false;
        }

        return correlationId.All(
            character =>
                char.IsLetterOrDigit(
                    character) ||
                character is '-' or '_' or '.');
    }
}