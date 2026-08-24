using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SprintBoard.api.Auth;
using SprintBoard.Domain.Entities;
using Xunit;

namespace SprintBoard.Test.Auth
{
    /// <summary>
    /// Contains unit tests for the <see cref="JwtTokenService"/>.
    /// </summary>
    public class JwtTokenServiceTests
    {
        private const string SecretKey =
            "SprintBoard.Tests.SuperSecret.Jwt.Key.2026.1234567890";

        private const string Issuer = "SprintBoard.Tests";
        private const string Audience = "SprintBoard.Tests.Client";

        // ============================================================
        // CREATE TOKEN
        // ============================================================

        [Fact]
        public void CreateToken_ShouldThrowArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            var exception = Assert.Throws<ArgumentNullException>(
                () => service.CreateToken(null!));

            // Assert
            Assert.Equal(
                "user",
                exception.ParamName);
        }

        [Fact]
        public void CreateToken_ShouldReturnValidSerializedJwt()
        {
            // Arrange
            var service = CreateService();
            var user = CreateUser();

            // Act
            var (token, expiresAtUtc) =
                service.CreateToken(user);

            // Assert
            Assert.False(
                string.IsNullOrWhiteSpace(token));

            Assert.True(
                expiresAtUtc > DateTime.UtcNow);

            var handler = new JwtSecurityTokenHandler();

            Assert.True(
                handler.CanReadToken(token));

            var jwt = handler.ReadJwtToken(token);

            Assert.NotNull(jwt);
        }

        [Fact]
        public void CreateToken_ShouldIncludeUserClaims()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(
                username: "diego0425",
                email: "diego@example.com");

            // Act
            var (token, _) =
                service.CreateToken(user);

            var jwt =
                new JwtSecurityTokenHandler()
                    .ReadJwtToken(token);

            // Assert
            Assert.Contains(
                jwt.Claims,
                claim =>
                    claim.Type == JwtRegisteredClaimNames.Sub &&
                    claim.Value == user.Id.ToString());

            Assert.Contains(
                jwt.Claims,
                claim =>
                    claim.Type == JwtRegisteredClaimNames.Email &&
                    claim.Value == "diego@example.com");

            Assert.Contains(
                jwt.Claims,
                claim =>
                    claim.Type == JwtRegisteredClaimNames.Name &&
                    claim.Value == "diego0425");

            Assert.Contains(
                jwt.Claims,
                claim =>
                    claim.Type == ClaimTypes.NameIdentifier &&
                    claim.Value == user.Id.ToString());

            Assert.Contains(
                jwt.Claims,
                claim =>
                    claim.Type == ClaimTypes.Email &&
                    claim.Value == "diego@example.com");

            Assert.Contains(
                jwt.Claims,
                claim =>
                    claim.Type == ClaimTypes.Name &&
                    claim.Value == "diego0425");
        }

        [Fact]
        public void CreateToken_ShouldUseConfiguredIssuerAndAudience()
        {
            // Arrange
            var service = CreateService();
            var user = CreateUser();

            // Act
            var (token, _) =
                service.CreateToken(user);

            var jwt =
                new JwtSecurityTokenHandler()
                    .ReadJwtToken(token);

            // Assert
            Assert.Equal(
                Issuer,
                jwt.Issuer);

            Assert.Contains(
                Audience,
                jwt.Audiences);
        }

        [Fact]
        public void CreateToken_ShouldUseConfiguredExpirationTime()
        {
            // Arrange
            const int expiresMinutes = 45;

            var service =
                CreateService(expiresMinutes);

            var user = CreateUser();

            var beforeCreation =
                DateTime.UtcNow.AddMinutes(expiresMinutes);

            // Act
            var (token, expiresAtUtc) =
                service.CreateToken(user);

            var afterCreation =
                DateTime.UtcNow.AddMinutes(expiresMinutes);

            var jwt =
                new JwtSecurityTokenHandler()
                    .ReadJwtToken(token);

            // Assert
            Assert.InRange(
                expiresAtUtc,
                beforeCreation,
                afterCreation);

            var expirationDifference =
                (jwt.ValidTo - expiresAtUtc).Duration();

            Assert.True(
                expirationDifference < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void CreateToken_ShouldGenerateTokenWithValidSignature()
        {
            // Arrange
            var service = CreateService();
            var user = CreateUser();

            var (token, _) =
                service.CreateToken(user);

            var validationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,

                    ValidIssuer = Issuer,
                    ValidAudience = Audience,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                SecretKey)),

                    ClockSkew = TimeSpan.Zero
                };

            var handler =
                new JwtSecurityTokenHandler();

            // Act
            var principal = handler.ValidateToken(
                token,
                validationParameters,
                out var validatedToken);

            // Assert
            Assert.NotNull(principal);
            Assert.NotNull(validatedToken);

            var jwt =
                Assert.IsType<JwtSecurityToken>(
                    validatedToken);

            Assert.Equal(
                SecurityAlgorithms.HmacSha256,
                jwt.Header.Alg);

            Assert.Equal(
                user.Id.ToString(),
                principal.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value);

            Assert.Equal(
                user.Email,
                principal.FindFirst(
                    ClaimTypes.Email)?.Value);

            Assert.Equal(
                user.Username,
                principal.FindFirst(
                    ClaimTypes.Name)?.Value);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        /// <summary>
        /// Creates a JwtTokenService using an isolated JWT configuration.
        /// </summary>
        private static JwtTokenService CreateService(
            int expiresMinutes = 60)
        {
            var options = Options.Create(
                new JwtOptions
                {
                    Key = SecretKey,
                    Issuer = Issuer,
                    Audience = Audience,
                    ExpiresMinutes = expiresMinutes
                });

            return new JwtTokenService(options);
        }

        /// <summary>
        /// Creates a valid user for JWT tests.
        /// </summary>
        private static User CreateUser(
            string username = "testuser",
            string email = "test@example.com")
        {
            return new User(
                "Test User",
                username,
                email,
                "password-hash");
        }
    }
}