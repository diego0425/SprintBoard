using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SprintBoard.api.Auth;
using SprintBoard.Application.DTOs.Auth;
using SprintBoard.Application.DTOs.Board;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace SprintBoard.Test.Integration
{
    /// <summary>
    /// Contains integration tests for complete authentication and
    /// board-management flows through the real HTTP pipeline.
    /// </summary>
    public sealed class BoardFlowIntegrationTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        /// <summary>
        /// Initializes the integration test class with the custom
        /// SprintBoard test application factory.
        /// </summary>
        /// <param name="factory">
        /// Factory hosting SprintBoard with an isolated SQLite database.
        /// </param>
        public BoardFlowIntegrationTests(
            CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // ============================================================
        // REGISTER
        // ============================================================

        /// <summary>
        /// Verifies that registering a valid user through the real
        /// HTTP pipeline returns a valid JWT access token.
        /// </summary>
        [Fact]
        public async Task Register_ShouldReturnAccessToken_WhenRequestIsValid()
        {
            // Arrange
            using var client =
                CreateClient();

            var request =
                CreateUniqueRegisterRequest();

            // Act
            var response =
                await client.PostAsJsonAsync(
                    "/api/v1/auth/register",
                    request,
                    TestContext.Current.CancellationToken);

            var body =
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"Expected 200 OK, but received " +
                $"{(int)response.StatusCode} {response.StatusCode}. " +
                $"Response body: {body}");

            var authResponse =
                await response.Content
                    .ReadFromJsonAsync<AuthResponse>(
                        cancellationToken:
                            TestContext.Current.CancellationToken);

            Assert.NotNull(authResponse);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    authResponse!.AccessToken));

            Assert.True(
                authResponse.ExpiresAtUtc >
                DateTime.UtcNow);
        }

        // ============================================================
        // LOGIN + PERSISTENCE
        // ============================================================

        /// <summary>
        /// Verifies that a user registered through the API is actually
        /// persisted and can subsequently authenticate through login.
        /// </summary>
        [Fact]
        public async Task Login_ShouldSucceed_AfterUserIsRegistered()
        {
            // Arrange
            using var client =
                CreateClient();

            var registerRequest =
                CreateUniqueRegisterRequest();

            var registerResponse =
                await client.PostAsJsonAsync(
                    "/api/v1/auth/register",
                    registerRequest,
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                registerResponse.StatusCode);

            var loginRequest =
                new LoginRequest
                {
                    Email =
                        registerRequest.Email,

                    Password =
                        registerRequest.Password
                };

            // Act
            var loginResponse =
                await client.PostAsJsonAsync(
                    "/api/v1/auth/login",
                    loginRequest,
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                loginResponse.StatusCode);

            var authResponse =
                await loginResponse.Content
                    .ReadFromJsonAsync<AuthResponse>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(authResponse);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    authResponse!.AccessToken));
        }

        // ============================================================
        // CREATE BOARD
        // ============================================================

        /// <summary>
        /// Verifies the complete Register -> JWT -> Create Board flow
        /// using the real HTTP pipeline and persistence infrastructure.
        /// </summary>
        [Fact]
        public async Task CreateBoard_ShouldReturnCreated_WhenUserIsAuthenticated()
        {
            // Arrange
            using var client =
                await CreateAuthenticatedClientAsync();

            var request =
                new CreateBoardRequest
                {
                    Name = "Integration Board"
                };

            // Act
            var response =
                await client.PostAsJsonAsync(
                    "/api/v1/boards",
                    request,
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);

            var board =
                await response.Content
                    .ReadFromJsonAsync<BoardResponse>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(board);

            Assert.NotEqual(
                Guid.Empty,
                board!.Id);

            Assert.Equal(
                "Integration Board",
                board.Name);

            Assert.NotEqual(
                Guid.Empty,
                board.OwnerId);

            Assert.NotNull(
                response.Headers.Location);

            Assert.Contains(
                board.Id.ToString(),
                response.Headers.Location!
                    .ToString());
        }

        // ============================================================
        // GET MY BOARDS
        // ============================================================

        /// <summary>
        /// Verifies that a board created through the HTTP API is
        /// persisted and appears in the authenticated user's board list.
        /// </summary>
        [Fact]
        public async Task GetMyBoards_ShouldReturnBoard_AfterBoardIsCreated()
        {
            // Arrange
            using var client =
                await CreateAuthenticatedClientAsync();

            var boardName =
                $"Board-{Guid.NewGuid():N}";

            var createRequest =
                new CreateBoardRequest
                {
                    Name = boardName
                };

            var createResponse =
                await client.PostAsJsonAsync(
                    "/api/v1/boards",
                    createRequest,
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode);

            var createdBoard =
                await createResponse.Content
                    .ReadFromJsonAsync<BoardResponse>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(createdBoard);

            // Act
            var response =
                await client.GetAsync(
                    "/api/v1/boards",
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var boards =
                await response.Content
                    .ReadFromJsonAsync<
                        List<BoardResponse>>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(boards);

            Assert.Contains(
                boards!,
                board =>
                    board.Id ==
                        createdBoard!.Id &&
                    board.Name ==
                        boardName);
        }

        // ============================================================
        // GET BOARD BY ID
        // ============================================================

        /// <summary>
        /// Verifies that a board created through the API can later
        /// be retrieved by its identifier using the same JWT identity.
        /// </summary>
        [Fact]
        public async Task GetBoardById_ShouldReturnCreatedBoard()
        {
            // Arrange
            using var client =
                await CreateAuthenticatedClientAsync();

            var boardName =
                $"Board-{Guid.NewGuid():N}";

            var createResponse =
                await client.PostAsJsonAsync(
                    "/api/v1/boards",
                    new CreateBoardRequest
                    {
                        Name = boardName
                    },
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode);

            var createdBoard =
                await createResponse.Content
                    .ReadFromJsonAsync<BoardResponse>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(createdBoard);

            // Act
            var response =
                await client.GetAsync(
                    $"/api/v1/boards/{createdBoard!.Id}",
                    TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var returnedBoard =
                await response.Content
                    .ReadFromJsonAsync<BoardResponse>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(returnedBoard);

            Assert.Equal(
                createdBoard.Id,
                returnedBoard!.Id);

            Assert.Equal(
                boardName,
                returnedBoard.Name);

            Assert.Equal(
                createdBoard.OwnerId,
                returnedBoard.OwnerId);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        /// <summary>
        /// Creates an HTTP client connected to the in-memory
        /// SprintBoard application.
        /// </summary>
        private HttpClient CreateClient()
        {
            return _factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });
        }

        /// <summary>
        /// Registers a unique user through the real HTTP API,
        /// obtains the generated JWT, and returns an authenticated client.
        /// </summary>
        private async Task<HttpClient>
            CreateAuthenticatedClientAsync()
        {
            var client =
                CreateClient();

            var registerRequest =
                CreateUniqueRegisterRequest();

            var response =
                await client.PostAsJsonAsync(
                    "/api/v1/auth/register",
                    registerRequest,
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var authResponse =
                await response.Content
                    .ReadFromJsonAsync<AuthResponse>(
                        cancellationToken:
                            TestContext.Current
                                .CancellationToken);

            Assert.NotNull(authResponse);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    authResponse!.AccessToken));

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    authResponse.AccessToken);

            return client;
        }

        /// <summary>
        /// Creates unique and valid registration data so integration
        /// tests can share the same test database safely.
        /// </summary>
        private static RegisterRequest
            CreateUniqueRegisterRequest()
        {
            var suffix =
                Guid.NewGuid()
                    .ToString("N");

            return new RegisterRequest
            {
                FullName =
                    "Integration Test User",

                Username =
                    $"user{suffix[..12]}",

                Email =
                    $"{suffix}@example.com",

                Password =
                    "Password123!",

                RepeatPassword =
                    "Password123!"
            };
        }
    }
}