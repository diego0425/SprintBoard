using System.Security.Claims;

namespace SprintBoard.api.Services;

/// <summary>
/// Reads information about the authenticated user from the current HTTP context.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentUserService"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">
    /// Accessor used to retrieve the current HTTP context and the authenticated user's claims.
    /// </param>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Returns the identifier of the currently authenticated user.
    /// </summary>
    /// <returns>
    /// Identifier extracted from the authenticated user's <see cref="ClaimTypes.NameIdentifier"/> claim.
    /// </returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the request is not authenticated or does not contain a valid user identifier claim.
    /// </exception>
    public Guid GetUserId()
    {
        var claimsPrincipal = _httpContextAccessor.HttpContext?.User;
        var userIdClaimValue = claimsPrincipal?
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(userIdClaimValue, out var currentUserId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user identifier.");
        }

        return currentUserId;
    }
}
