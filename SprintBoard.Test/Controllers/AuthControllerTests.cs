using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using SprintBoard.api.Auth;
using SprintBoard.api.Controllers;
using SprintBoard.Application.DTOs.Auth;
using SprintBoard.Application.Interfaces;
using SprintBoard.Application.Services;
using SprintBoard.Domain.Entities;
using Xunit;

namespace SprintBoard.Test.Controllers
{
    /// <summary>
    /// Contains tests for the <see cref="AuthController"/>.
    /// </summary>
    public class AuthControllerTests
    {
        private const string JwtKey =
            "SprintBoard.Tests.AuthController.Secret.Key.2026.123456789";

        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly AuthService _authService;
        private readonly JwtTokenService _jwtTokenService;
        private readonly AuthController _controller;

        /// <summary>
        /// Initializes the dependencies used by the controller tests.
        /// </summary>
        public AuthControllerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();

            _authService = new AuthService(
                _userRepositoryMock.Object);

            _jwtTokenService = new JwtTokenService(
                Options.Create(
                    new JwtOptions
                    {
                        Key = JwtKey,
                        Issuer = "SprintBoard.Tests",
                        Audience = "SprintBoard.Tests.Client",
                        ExpiresMinutes = 60
                    }));

            _controller = new AuthController(
                _authService,
                _jwtTokenService);
        }

        // ============================================================
        // REGISTER
        // ============================================================

        /// <summary>
        /// Verifies that Register returns an HTTP 200 response containing
        /// a valid authentication response when the request is valid.
        /// </summary>
        [Fact]
        public async Task Register_ShouldReturnOkWithAuthResponse_WhenRequestIsValid()
        {
            // Arrange
            var request = CreateValidRegisterRequest();

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("user@example.com"))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _controller.Register(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(
                result.Result);

            var response = Assert.IsType<AuthResponse>(
                okResult.Value);

            Assert.False(
                string.IsNullOrWhiteSpace(response.AccessToken));

            Assert.True(
                response.ExpiresAtUtc > DateTime.UtcNow);

            Assert.Equal(
                200,
                okResult.StatusCode);
        }

        /// <summary>
        /// Verifies that Register persists the newly created user
        /// when the registration request is valid.
        /// </summary>
        [Fact]
        public async Task Register_ShouldPersistNewUser()
        {
            // Arrange
            var request = CreateValidRegisterRequest();

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("user@example.com"))
                .ReturnsAsync((User?)null);

            // Act
            await _controller.Register(request);

            // Assert
            _userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<User>(user =>
                        user.FullName == "Test User" &&
                        user.Username == "testuser" &&
                        user.Email == "user@example.com")),
                Times.Once);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Register returns a JWT containing the claims
        /// of the newly registered user.
        /// </summary>
        [Fact]
        public async Task Register_ShouldReturnTokenContainingCreatedUserClaims()
        {
            // Arrange
            var request = new RegisterRequest
            {
                FullName = "Diego Sousa",
                Username = "diego0425",
                Email = "DIEGO@EXAMPLE.COM",
                Password = "Password123",
                RepeatPassword = "Password123"
            };

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("diego@example.com"))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _controller.Register(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(
                result.Result);

            var response = Assert.IsType<AuthResponse>(
                okResult.Value);

            var jwt = new JwtSecurityTokenHandler()
                .ReadJwtToken(response.AccessToken);

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
        }

        /// <summary>
        /// Verifies that Register propagates an ArgumentException
        /// when the registration request contains invalid data.
        /// </summary>
        [Fact]
        public async Task Register_ShouldPropagateArgumentException_WhenRequestIsInvalid()
        {
            // Arrange
            var request = new RegisterRequest
            {
                FullName = "Test User",
                Username = "testuser",
                Email = "user@example.com",
                Password = "123",
                RepeatPassword = "123"
            };

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _controller.Register(request));

            // Assert
            Assert.Equal(
                "Password must be at least 8 characters.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<User>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Register propagates an InvalidOperationException
        /// when the provided email address is already registered.
        /// </summary>
        [Fact]
        public async Task Register_ShouldPropagateInvalidOperationException_WhenEmailAlreadyExists()
        {
            // Arrange
            var existingUser = CreateUser();
            var request = CreateValidRegisterRequest();

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("user@example.com"))
                .ReturnsAsync(existingUser);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _controller.Register(request));

            // Assert
            Assert.Equal(
                "Email already in use.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<User>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // LOGIN
        // ============================================================

        /// <summary>
        /// Verifies that Login returns an HTTP 200 response containing
        /// a valid authentication response when the credentials are valid.
        /// </summary>
        [Fact]
        public async Task Login_ShouldReturnOkWithAuthResponse_WhenCredentialsAreValid()
        {
            // Arrange
            const string password = "Password123";

            var user = CreateUser(
                passwordHash: HashPassword(password));

            var request = new LoginRequest
            {
                Email = " USER@EXAMPLE.COM ",
                Password = password
            };

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("user@example.com"))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(
                result.Result);

            var response = Assert.IsType<AuthResponse>(
                okResult.Value);

            Assert.Equal(
                200,
                okResult.StatusCode);

            Assert.False(
                string.IsNullOrWhiteSpace(response.AccessToken));

            Assert.True(
                response.ExpiresAtUtc > DateTime.UtcNow);
        }

        /// <summary>
        /// Verifies that Login returns a JWT containing the claims
        /// of the successfully authenticated user.
        /// </summary>
        [Fact]
        public async Task Login_ShouldReturnTokenContainingAuthenticatedUserClaims()
        {
            // Arrange
            const string password = "Password123";

            var user = CreateUser(
                username: "diego0425",
                email: "diego@example.com",
                passwordHash: HashPassword(password));

            var request = new LoginRequest
            {
                Email = "diego@example.com",
                Password = password
            };

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("diego@example.com"))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(
                result.Result);

            var response = Assert.IsType<AuthResponse>(
                okResult.Value);

            var jwt = new JwtSecurityTokenHandler()
                .ReadJwtToken(response.AccessToken);

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
        }

        /// <summary>
        /// Verifies that Login does not persist or modify user data
        /// when authentication succeeds.
        /// </summary>
        [Fact]
        public async Task Login_ShouldNotPersistAnything()
        {
            // Arrange
            const string password = "Password123";

            var user = CreateUser(
                passwordHash: HashPassword(password));

            var request = new LoginRequest
            {
                Email = user.Email,
                Password = password
            };

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync(user.Email))
                .ReturnsAsync(user);

            // Act
            await _controller.Login(request);

            // Assert
            _userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<User>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Login propagates an ArgumentException
        /// when the email and password are missing.
        /// </summary>
        [Fact]
        public async Task Login_ShouldPropagateArgumentException_WhenCredentialsAreEmpty()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "",
                Password = ""
            };

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _controller.Login(request));

            // Assert
            Assert.Equal(
                "Email and password are required.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByEmailAsync(
                    It.IsAny<string>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Login propagates an InvalidOperationException
        /// when the provided credentials are invalid.
        /// </summary>
        [Fact]
        public async Task Login_ShouldPropagateInvalidOperationException_WhenCredentialsAreInvalid()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "missing@example.com",
                Password = "Password123"
            };

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("missing@example.com"))
                .ReturnsAsync((User?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _controller.Login(request));

            // Assert
            Assert.Equal(
                "Invalid credentials.",
                exception.Message);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        /// <summary>
        /// Creates a valid registration request for controller tests.
        /// </summary>
        private static RegisterRequest CreateValidRegisterRequest()
        {
            return new RegisterRequest
            {
                FullName = "Test User",
                Username = "testuser",
                Email = "user@example.com",
                Password = "Password123",
                RepeatPassword = "Password123"
            };
        }

        /// <summary>
        /// Creates a valid user for authentication controller tests.
        /// </summary>
        private static User CreateUser(
            string username = "testuser",
            string email = "user@example.com",
            string passwordHash = "password-hash")
        {
            return new User(
                "Test User",
                username,
                email,
                passwordHash);
        }

        /// <summary>
        /// Generates a password hash using the same SHA-256 algorithm
        /// currently used by the authentication service.
        /// </summary>
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();

            var hashBytes = sha256.ComputeHash(
                Encoding.UTF8.GetBytes(password));

            return Convert.ToHexString(hashBytes);
        }
    }
}