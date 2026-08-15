namespace SprintBoard.api.Auth;

/// <summary>
/// Defines the configuration required to issue and validate JWT access tokens.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// Gets the secret key used to sign JWT access tokens.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets the expected token issuer.
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Gets the expected token audience.
    /// </summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Gets the access token lifetime, expressed in minutes.
    /// </summary>
    public int ExpiresMinutes { get; init; }
}
