namespace SprintBoard.api.Services;

/// <summary>
/// Defines configuration used by the local file storage service.
/// </summary>
public sealed class FileStorageOptions
{
    /// <summary>
    /// Gets the public base URL used to build links
    /// for files exposed by the application.
    /// </summary>
    public string PublicBaseUrl { get; init; } =
        string.Empty;
}