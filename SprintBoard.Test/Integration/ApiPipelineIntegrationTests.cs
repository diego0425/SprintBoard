using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SprintBoard.Test.Integration
{
    /// <summary>
    /// Contains integration tests that exercise the real ASP.NET Core
    /// HTTP pipeline through an in-memory test server.
    /// </summary>
    public class ApiPipelineIntegrationTests
        : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        /// <summary>
        /// Initializes an HTTP client connected to the SprintBoard
        /// application running through WebApplicationFactory.
        /// </summary>
        /// <param name="factory">
        /// Factory responsible for hosting the application in memory.
        /// </param>
        public ApiPipelineIntegrationTests(
            WebApplicationFactory<Program> factory)
        {
            _client =
                factory.CreateClient(
                    new WebApplicationFactoryClientOptions
                    {
                        AllowAutoRedirect = false
                    });
        }

        // ============================================================
        // AUTHORIZATION PIPELINE
        // ============================================================

        /// <summary>
        /// Verifies that the users profile endpoint returns HTTP 401
        /// when the request does not contain a JWT access token.
        /// </summary>
        [Fact]
        public async Task GetCurrentUser_ShouldReturnUnauthorized_WhenTokenIsMissing()
        {
            // Arrange
            const string endpoint =
                "/api/v1/users/me";

            // Act
            var response =
                await _client.GetAsync(
                    endpoint,
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        /// <summary>
        /// Verifies that the boards endpoint returns HTTP 401
        /// when the request does not contain a JWT access token.
        /// </summary>
        [Fact]
        public async Task GetBoards_ShouldReturnUnauthorized_WhenTokenIsMissing()
        {
            // Arrange
            const string endpoint =
                "/api/v1/boards";

            // Act
            var response =
                await _client.GetAsync(
                    endpoint,
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        // ============================================================
        // GLOBAL EXCEPTION PIPELINE
        // ============================================================

        /// <summary>
        /// Verifies that an ArgumentException thrown by the real
        /// registration service travels through the HTTP pipeline
        /// and is converted into HTTP 400 by the exception middleware.
        /// </summary>
        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenFullNameIsEmpty()
        {
            // Arrange
            const string endpoint =
                "/api/v1/auth/register";

            var request =
                new
                {
                    FullName = "",
                    Username = "integrationuser",
                    Email = "integration@example.com",
                    Password = "Password123",
                    RepeatPassword = "Password123"
                };

            // Act
            var response =
                await _client.PostAsJsonAsync(
                    endpoint,
                    request,
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);

            var body =
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken);

            Assert.Contains(
                "Full name cannot be empty.",
                body);

            Assert.StartsWith(
                "application/json",
                response.Content.Headers
                    .ContentType?
                    .MediaType ??
                string.Empty);
        }

        /// <summary>
        /// Verifies that invalid login data travels through the real
        /// service and middleware pipeline and produces HTTP 400.
        /// </summary>
        [Fact]
        public async Task Login_ShouldReturnBadRequest_WhenCredentialsAreEmpty()
        {
            // Arrange
            const string endpoint =
                "/api/v1/auth/login";

            var request =
                new
                {
                    Email = "",
                    Password = ""
                };

            // Act
            var response =
                await _client.PostAsJsonAsync(
                    endpoint,
                    request,
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);

            var body =
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken);

            Assert.Contains(
                "Email and password are required.",
                body);
        }

        // ============================================================
        // MODEL BINDING
        // ============================================================

        /// <summary>
        /// Verifies that malformed JSON is rejected by the ASP.NET Core
        /// request pipeline before reaching the application service.
        /// </summary>
        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenJsonIsMalformed()
        {
            // Arrange
            const string endpoint =
                "/api/v1/auth/register";

            using var content =
                new StringContent(
                    "{ invalid-json",
                    Encoding.UTF8,
                    "application/json");

            // Act
            var response =
                await _client.PostAsync(
                    endpoint,
                    content,
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.BadRequest,
                response.StatusCode);
        }

        // ============================================================
        // ROUTING
        // ============================================================

        /// <summary>
        /// Verifies that a request to an unknown API route produces
        /// the standard HTTP 404 Not Found response.
        /// </summary>
        [Fact]
        public async Task UnknownEndpoint_ShouldReturnNotFound()
        {
            // Arrange
            const string endpoint =
                "/api/v1/this-endpoint-does-not-exist";

            // Act
            var response =
                await _client.GetAsync(
                    endpoint,
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }
    }
}