using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SprintBoard.Domain.Entities;

namespace SprintBoard.api.Auth;

/// <summary>
/// Creates signed JWT access tokens for authenticated users.
/// </summary>
public sealed class JwtTokenService
{
    private readonly JwtOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    /// <param name="options">
    /// Options accessor that provides the JWT configuration used to build and sign tokens.
    /// </param>
    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Creates a signed JWT access token for the specified user.
    /// </summary>
    /// <param name="user">
    /// Authenticated user whose identity claims will be embedded in the generated token.
    /// </param>
    /// <returns>
    /// A tuple containing the serialized JWT access token and its expiration date in UTC.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="user"/> is <see langword="null"/>.
    /// </exception>
    public (string Token, DateTime ExpiresAtUtc) CreateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var issuedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = issuedAtUtc.AddMinutes(_options.ExpiresMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var jwtToken = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        var serializedToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        return (serializedToken, expiresAtUtc);
    }
}
