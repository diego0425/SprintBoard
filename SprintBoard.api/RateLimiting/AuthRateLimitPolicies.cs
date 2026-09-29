namespace SprintBoard.api.RateLimiting;

/// <summary>
/// Contains the named rate-limiting policies used by
/// authentication endpoints.
/// </summary>
public static class AuthRateLimitPolicies
{
    /// <summary>
    /// Rate-limiting policy applied to login requests.
    /// </summary>
    public const string Login =
        "auth-login";

    /// <summary>
    /// Rate-limiting policy applied to registration requests.
    /// </summary>
    public const string Register =
        "auth-register";
}