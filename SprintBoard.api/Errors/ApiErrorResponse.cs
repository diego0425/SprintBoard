namespace SprintBoard.api.Errors;

/// <summary>
/// Represents the standardized error payload returned by the API.
/// </summary>
public sealed class ApiErrorResponse
{
    /// <summary>
    /// Gets the HTTP status code associated with the error.
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Gets the message that describes the error condition.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the request trace identifier used to correlate the response with server logs.
    /// </summary>
    public string? TraceId { get; init; }
}
