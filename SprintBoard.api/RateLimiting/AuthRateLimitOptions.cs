namespace SprintBoard.api.RateLimiting;

/// <summary>
/// Defines rate-limiting configuration for authentication endpoints.
/// </summary>
public sealed class AuthRateLimitOptions
{
    /// <summary>
    /// Gets the configuration section containing authentication
    /// rate-limiting settings.
    /// </summary>
    public const string SectionName =
        "RateLimiting:Auth";

    /// <summary>
    /// Gets or sets the number of login requests allowed
    /// during a single rate-limit window.
    /// </summary>
    public int LoginPermitLimit
    {
        get;
        set;
    } = 10;

    /// <summary>
    /// Gets or sets the login rate-limit window in seconds.
    /// </summary>
    public int LoginWindowSeconds
    {
        get;
        set;
    } = 60;

    /// <summary>
    /// Gets or sets the number of registration requests
    /// allowed during a single rate-limit window.
    /// </summary>
    public int RegisterPermitLimit
    {
        get;
        set;
    } = 5;

    /// <summary>
    /// Gets or sets the registration rate-limit window
    /// in seconds.
    /// </summary>
    public int RegisterWindowSeconds
    {
        get;
        set;
    } = 600;
}