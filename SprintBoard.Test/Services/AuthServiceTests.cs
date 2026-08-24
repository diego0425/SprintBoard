using Moq;
using SprintBoard.Application.DTOs.Auth;
using SprintBoard.Application.Interfaces;
using SprintBoard.Application.Services;
using SprintBoard.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace SprintBoard.Test.Services
{
    /// <summary>
    /// Contains unit tests for the <see cref="AuthService"/>.
    /// </summary>
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly AuthService _service;

        /// <summary>
        /// Initializes the mocked dependencies and service instance.
        /// </summary>
        public AuthServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();

            _service = new AuthService(
                _userRepositoryMock.Object);
        }

        // ============================================================
        // REGISTER
        // ============================================================

        [Fact]
        public async Task RegisterAsync_ShouldThrowArgumentException_WhenFullNameIsEmpty()
        {
            // Arrange
            var request = CreateValidRegisterRequest(
                fullName: "   ");

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RegisterAsync(request));

            // Assert
            Assert.Equal(
                "Full name cannot be empty.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByEmailAsync(
                    It.IsAny<string>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<User>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowArgumentException_WhenUsernameIsEmpty()
        {
            // Arrange
            var request = CreateValidRegisterRequest(
                username: "   ");

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RegisterAsync(request));

            // Assert
            Assert.Equal(
                "Username cannot be empty.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByEmailAsync(
                    It.IsAny<string>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<User>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowArgumentException_WhenEmailIsEmpty()
        {
            // Arrange
            var request = CreateValidRegisterRequest(
                email: "   ");

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RegisterAsync(request));

            // Assert
            Assert.Equal(
                "Email cannot be empty.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByEmailAsync(
                    It.IsAny<string>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<User>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowArgumentException_WhenPasswordIsEmpty()
        {
            // Arrange
            var request = CreateValidRegisterRequest(
                password: "",
                repeatPassword: "");

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RegisterAsync(request));

            // Assert
            Assert.Equal(
                "Password must be at least 8 characters.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByEmailAsync(
                    It.IsAny<string>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<User>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowArgumentException_WhenPasswordIsTooShort()
        {
            // Arrange
            var request = CreateValidRegisterRequest(
                password: "1234567",
                repeatPassword: "1234567");

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RegisterAsync(request));

            // Assert
            Assert.Equal(
                "Password must be at least 8 characters.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByEmailAsync(
                    It.IsAny<string>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<User>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowArgumentException_WhenPasswordsDoNotMatch()
        {
            // Arrange
            var request = CreateValidRegisterRequest(
                password: "Password123",
                repeatPassword: "Different123");

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RegisterAsync(request));

            // Assert
            Assert.Equal(
                "Passwords do not match.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByEmailAsync(
                    It.IsAny<string>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<User>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowInvalidOperationException_WhenEmailIsAlreadyInUse()
        {
            // Arrange
            var existingUser = new User(
                "Existing User",
                "existing",
                "existing@example.com",
                HashPassword("Password123"));

            var request = CreateValidRegisterRequest(
                email: " Existing@Example.COM ");

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("existing@example.com"))
                .ReturnsAsync(existingUser);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.RegisterAsync(request));

            // Assert
            Assert.Equal(
                "Email already in use.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByEmailAsync("existing@example.com"),
                Times.Once);

            _userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<User>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldCreateNormalizeHashAndSaveUser()
        {
            // Arrange
            const string password = "Password123";

            var request = new RegisterRequest
            {
                FullName = "   Diego Sousa   ",
                Username = "   diego0425   ",
                Email = "   DIEGO@EXAMPLE.COM   ",
                Password = password,
                RepeatPassword = password
            };

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("diego@example.com"))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _service.RegisterAsync(request);

            // Assert
            Assert.NotEqual(
                Guid.Empty,
                result.Id);

            Assert.Equal(
                "Diego Sousa",
                result.FullName);

            Assert.Equal(
                "diego0425",
                result.Username);

            Assert.Equal(
                "diego@example.com",
                result.Email);

            Assert.Equal(
                HashPassword(password),
                result.PasswordHash);

            Assert.NotEqual(
                default,
                result.CreatedAt);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByEmailAsync("diego@example.com"),
                Times.Once);

            _userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<User>(user =>
                        user.FullName == "Diego Sousa" &&
                        user.Username == "diego0425" &&
                        user.Email == "diego@example.com" &&
                        user.PasswordHash == HashPassword(password))),
                Times.Once);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        // ============================================================
        // LOGIN
        // ============================================================

        [Fact]
        public async Task LoginAsync_ShouldThrowArgumentException_WhenEmailIsEmpty()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "   ",
                Password = "Password123"
            };

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.LoginAsync(request));

            // Assert
            Assert.Equal(
                "Email and password are required.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByEmailAsync(
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowArgumentException_WhenPasswordIsEmpty()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "user@example.com",
                Password = "   "
            };

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.LoginAsync(request));

            // Assert
            Assert.Equal(
                "Email and password are required.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByEmailAsync(
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowInvalidOperationException_WhenUserDoesNotExist()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = " Missing@Example.COM ",
                Password = "Password123"
            };

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("missing@example.com"))
                .ReturnsAsync((User?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.LoginAsync(request));

            // Assert
            Assert.Equal(
                "Invalid credentials.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByEmailAsync("missing@example.com"),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowInvalidOperationException_WhenPasswordIsIncorrect()
        {
            // Arrange
            var user = new User(
                "Test User",
                "testuser",
                "user@example.com",
                HashPassword("CorrectPassword"));

            var request = new LoginRequest
            {
                Email = "user@example.com",
                Password = "WrongPassword"
            };

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("user@example.com"))
                .ReturnsAsync(user);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.LoginAsync(request));

            // Assert
            Assert.Equal(
                "Invalid credentials.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByEmailAsync("user@example.com"),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnUser_WhenCredentialsAreValid()
        {
            // Arrange
            const string password = "Password123";

            var user = new User(
                "Test User",
                "testuser",
                "user@example.com",
                HashPassword(password));

            var request = new LoginRequest
            {
                Email = "   USER@EXAMPLE.COM   ",
                Password = password
            };

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("user@example.com"))
                .ReturnsAsync(user);

            // Act
            var result = await _service.LoginAsync(request);

            // Assert
            Assert.Same(
                user,
                result);

            Assert.Equal(
                user.Id,
                result.Id);

            Assert.Equal(
                "user@example.com",
                result.Email);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByEmailAsync("user@example.com"),
                Times.Once);

            _userRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<User>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        /// <summary>
        /// Creates a valid registration request, allowing individual
        /// properties to be overridden by each test.
        /// </summary>
        private static RegisterRequest CreateValidRegisterRequest(
            string fullName = "Test User",
            string username = "testuser",
            string email = "user@example.com",
            string password = "Password123",
            string? repeatPassword = null)
        {
            return new RegisterRequest
            {
                FullName = fullName,
                Username = username,
                Email = email,
                Password = password,
                RepeatPassword = repeatPassword ?? password
            };
        }

        /// <summary>
        /// Creates the SHA-256 password hash used by the current
        /// authentication implementation.
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