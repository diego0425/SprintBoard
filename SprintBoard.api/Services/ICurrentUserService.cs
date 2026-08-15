namespace SprintBoard.api.Services;

/// <summary>
/// Provides access to information about the authenticated user.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Returns the identifier of the currently authenticated user.
    /// </summary>
    /// <returns>
    /// Identifier extracted from the authenticated user's claims principal.
    /// </returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the current request does not contain a valid authenticated user identifier.
    /// </exception>
    Guid GetUserId();
}
