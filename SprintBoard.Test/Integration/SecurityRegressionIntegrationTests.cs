using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using SprintBoard.api.Errors;
using SprintBoard.api.Middlewares;
using SprintBoard.Application.DTOs.Auth;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace SprintBoard.Test.Integration
{
    /// <summary>
    /// Contains HTTP-level regression tests for security controls
    /// applied to the SprintBoard request pipeline.
    /// </summary>
    public sealed class SecurityRegressionIntegrationTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory
            _factory;

        /// <summary>
        /// Initializes the security regression tests using
        /// the standard isolated application factory.
        /// </summary>
        public SecurityRegressionIntegrationTests(
            CustomWebApplicationFactory factory)
        {
            _factory =
                factory;
        }

        // ============================================================
        // RATE LIMITING
        // ============================================================

        /// <summary>
        /// Verifies that repeated login requests are rejected
        /// with HTTP 429 after the configured limit is exceeded.
        /// </summary>
        [Fact]
        public async Task Login_ShouldReturnTooManyRequests_WhenRateLimitIsExceeded()
        {
            // Arrange
            using var factory =
                CreateRateLimitedFactory(
                    loginPermitLimit:
                        2);

            using var client =
                factory.CreateClient(
                    new WebApplicationFactoryClientOptions
                    {
                        AllowAutoRedirect =
                            false
                    });

            var request =
                new
                {
                    Email =
                        "",

                    Password =
                        ""
                };

            // Act
            var firstResponse =
                await client.PostAsJsonAsync(
                    "/api/v1/auth/login",
                    request,
                    TestContext.Current
                        .CancellationToken);

            var secondResponse =
                await client.PostAsJsonAsync(
                    "/api/v1/auth/login",
                    request,
                    TestContext.Current
                        .CancellationToken);

            var thirdResponse =
                await client.PostAsJsonAsync(
                    "/api/v1/auth/login",
                    request,
                    TestContext.Current
                        .CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.BadRequest,
                firstResponse.StatusCode);

            Assert.Equal(
                HttpStatusCode.BadRequest,
                secondResponse.StatusCode);

            Assert.Equal(
                HttpStatusCode.TooManyRequests,
                thirdResponse.StatusCode);

            var error =
                await thirdResponse.Content
                    .ReadFromJsonAsync<
                        ApiErrorResponse>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(
                error);

            Assert.Equal(
                StatusCodes
                    .Status429TooManyRequests,
                error!.StatusCode);

            Assert.Equal(
                "Too many requests. " +
                "Please try again later.",
                error.Message);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    error.TraceId));

            Assert.True(
                thirdResponse.Headers.Contains(
                    CorrelationIdMiddleware
                        .HeaderName));
        }

        /// <summary>
        /// Verifies that repeated registration requests are
        /// independently protected by their own rate-limit policy.
        /// </summary>
        [Fact]
        public async Task Register_ShouldReturnTooManyRequests_WhenRateLimitIsExceeded()
        {
            // Arrange
            using var factory =
                CreateRateLimitedFactory(
                    registerPermitLimit:
                        1);

            using var client =
                factory.CreateClient(
                    new WebApplicationFactoryClientOptions
                    {
                        AllowAutoRedirect =
                            false
                    });

            var request =
                new
                {
                    FullName =
                        "",

                    Username =
                        "securityuser",

                    Email =
                        "security@example.com",

                    Password =
                        "Password123",

                    RepeatPassword =
                        "Password123"
                };

            // Act
            var firstResponse =
                await client.PostAsJsonAsync(
                    "/api/v1/auth/register",
                    request,
                    TestContext.Current
                        .CancellationToken);

            var secondResponse =
                await client.PostAsJsonAsync(
                    "/api/v1/auth/register",
                    request,
                    TestContext.Current
                        .CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.BadRequest,
                firstResponse.StatusCode);

            Assert.Equal(
                HttpStatusCode.TooManyRequests,
                secondResponse.StatusCode);
        }

        // ============================================================
        // FORWARDED HEADERS
        // ============================================================

        /// <summary>
        /// Verifies that requests arriving through the configured
        /// trusted proxy are rate limited by the forwarded client IP.
        /// </summary>
        [Fact]
        public async Task TrustedProxy_ShouldRateLimitClients_ByForwardedIpAddress()
        {
            // Arrange
            using var factory =
                CreateRateLimitedFactory(
                    loginPermitLimit:
                        1,
                    knownProxyIp:
                        "172.30.0.10");

            const string proxyIp =
                "172.30.0.10";

            const string firstClientIp =
                "203.0.113.10";

            const string secondClientIp =
                "203.0.113.11";

            // Act
            var firstClientRequest =
                await SendLoginFromProxyAsync(
                    factory,
                    proxyIp,
                    firstClientIp);

            var repeatedFirstClientRequest =
                await SendLoginFromProxyAsync(
                    factory,
                    proxyIp,
                    firstClientIp);

            var secondClientRequest =
                await SendLoginFromProxyAsync(
                    factory,
                    proxyIp,
                    secondClientIp);

            // Assert
            Assert.Equal(
                StatusCodes.Status400BadRequest,
                firstClientRequest);

            Assert.Equal(
                StatusCodes
                    .Status429TooManyRequests,
                repeatedFirstClientRequest);

            /*
             * A different forwarded client must receive
             * its own rate-limit partition.
             */
            Assert.Equal(
                StatusCodes.Status400BadRequest,
                secondClientRequest);
        }

        /// <summary>
        /// Verifies that an untrusted remote host cannot bypass
        /// rate limiting by spoofing X-Forwarded-For values.
        /// </summary>
        [Fact]
        public async Task UntrustedProxy_ShouldNotBypassRateLimit_WithSpoofedForwardedIp()
        {
            // Arrange
            using var factory =
                CreateRateLimitedFactory(
                    loginPermitLimit:
                        1,
                    knownProxyIp:
                        "172.30.0.10");

            const string untrustedProxyIp =
                "198.51.100.200";

            // Act
            var firstRequest =
                await SendLoginFromProxyAsync(
                    factory,
                    untrustedProxyIp,
                    "203.0.113.10");

            var spoofedRequest =
                await SendLoginFromProxyAsync(
                    factory,
                    untrustedProxyIp,
                    "203.0.113.11");

            // Assert
            Assert.Equal(
                StatusCodes.Status400BadRequest,
                firstRequest);

            /*
             * The forwarded IP must be ignored because the
             * immediate remote address is not a trusted proxy.
             */
            Assert.Equal(
                StatusCodes
                    .Status429TooManyRequests,
                spoofedRequest);
        }

        // ============================================================
        // FILE UPLOAD
        // ============================================================

        /// <summary>
        /// Verifies that a file claiming to be JPEG is rejected
        /// by the real HTTP pipeline when its bytes are not JPEG.
        /// </summary>
        [Fact]
        public async Task ProfileImageUpload_ShouldReturnBadRequest_WhenJpegContentIsSpoofed()
        {
            // Arrange
            using var client =
                _factory.CreateClient(
                    new WebApplicationFactoryClientOptions
                    {
                        AllowAutoRedirect =
                            false
                    });

            var accessToken =
                await RegisterUserAsync(
                    client);

            client.DefaultRequestHeaders
                .Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            using var multipartContent =
                new MultipartFormDataContent();

            byte[] maliciousContent =
            [
                0x3C,
                0x68,
                0x74,
                0x6D,
                0x6C,
                0x3E
            ];

            using var fileContent =
                new ByteArrayContent(
                    maliciousContent);

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(
                    "image/jpeg");

            multipartContent.Add(
                fileContent,
                "file",
                "profile.jpg");

            // Act
            var response =
                await client.PatchAsync(
                    "/api/v1/users/me/profile-image",
                    multipartContent,
                    TestContext.Current
                        .CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);

            var body =
                await response.Content
                    .ReadAsStringAsync(
                        TestContext.Current
                            .CancellationToken);

            Assert.Contains(
                "File content does not match " +
                "the declared image type.",
                body);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        /// <summary>
        /// Creates an isolated application instance with small
        /// authentication rate limits for regression testing.
        /// </summary>
        private static CustomWebApplicationFactory
            CreateRateLimitedFactory(
                int loginPermitLimit =
                    10_000,
                int registerPermitLimit =
                    10_000,
                string? knownProxyIp =
                    null)
        {
            var settings =
                new Dictionary<string, string?>
                {
                    [
                        "RateLimiting:Auth:" +
                        "LoginPermitLimit"
                    ] =
                        loginPermitLimit
                            .ToString(),

                    [
                        "RateLimiting:Auth:" +
                        "LoginWindowSeconds"
                    ] =
                        "60",

                    [
                        "RateLimiting:Auth:" +
                        "RegisterPermitLimit"
                    ] =
                        registerPermitLimit
                            .ToString(),

                    [
                        "RateLimiting:Auth:" +
                        "RegisterWindowSeconds"
                    ] =
                        "60"
                };

            if (!string.IsNullOrWhiteSpace(
                    knownProxyIp))
            {
                settings[
                    "ReverseProxy:KnownProxyIp"] =
                    knownProxyIp;
            }

            return new CustomWebApplicationFactory(
                settings);
        }

        /// <summary>
        /// Sends an invalid login request while explicitly
        /// controlling the immediate and forwarded client IPs.
        /// </summary>
        private static async Task<int>
            SendLoginFromProxyAsync(
                CustomWebApplicationFactory factory,
                string remoteProxyIp,
                string forwardedClientIp)
        {
            byte[] payload =
                Encoding.UTF8.GetBytes(
                    """
                    {
                      "email": "",
                      "password": ""
                    }
                    """);

            using var requestBody =
                new MemoryStream(
                    payload);

            var responseContext =
                await factory.Server.SendAsync(
                    context =>
                    {
                        context.Connection
                            .RemoteIpAddress =
                            IPAddress.Parse(
                                remoteProxyIp);

                        context.Request.Method =
                            HttpMethods.Post;

                        context.Request.Path =
                            "/api/v1/auth/login";

                        context.Request.ContentType =
                            "application/json";

                        context.Request.ContentLength =
                            payload.Length;

                        context.Request.Body =
                            requestBody;

                        context.Request.Headers[
                            "X-Forwarded-For"] =
                            forwardedClientIp;
                    },
                    TestContext.Current
                        .CancellationToken);

            return responseContext.Response
                .StatusCode;
        }

        /// <summary>
        /// Registers a unique user through the real HTTP API
        /// and returns the generated authentication token.
        /// </summary>
        private static async Task<string>
            RegisterUserAsync(
                HttpClient client)
        {
            var suffix =
                Guid.NewGuid()
                    .ToString("N")[..8];

            var request =
                new
                {
                    FullName =
                        "Security Test User",

                    Username =
                        $"security{suffix}",

                    Email =
                        $"security{suffix}@example.com",

                    Password =
                        "Password123",

                    RepeatPassword =
                        "Password123"
                };

            var response =
                await client.PostAsJsonAsync(
                    "/api/v1/auth/register",
                    request,
                    TestContext.Current
                        .CancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var authResponse =
                await response.Content
                    .ReadFromJsonAsync<
                        AuthResponse>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(
                authResponse);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    authResponse!.AccessToken));

            return authResponse.AccessToken;
        }
    }
}