using Moq;
using SprintBoard.Application.DTOs.User;
using SprintBoard.Application.Interfaces;
using SprintBoard.Application.Services;
using SprintBoard.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace SprintBoard.Test.Services
{
    /// <summary>
    /// Contains unit tests for the <see cref="UserService"/>.
    /// </summary>
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IFileStorageService> _fileStorageServiceMock;
        private readonly UserService _service;

        /// <summary>
        /// Initializes the mocked dependencies and service instance.
        /// </summary>
        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _fileStorageServiceMock = new Mock<IFileStorageService>();

            _service = new UserService(
                _userRepositoryMock.Object,
                _fileStorageServiceMock.Object);
        }

        // ============================================================
        // GET BY ID
        // ============================================================

        [Fact]
        public async Task GetByIdAsync_ShouldThrowArgumentException_WhenUserIdIsEmpty()
        {
            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetByIdAsync(Guid.Empty));

            // Assert
            Assert.Equal(
                "UserId cannot be empty.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.GetByIdAsync(userId));

            // Assert
            Assert.Equal(
                "User not found.",
                exception.Message);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedUser_WhenUserExists()
        {
            // Arrange
            var user = CreateUser();

            user.UpdateProfileImage(
                "https://example.com/profile.jpg");

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            var result = await _service.GetByIdAsync(user.Id);

            // Assert
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(user.Username, result.Username);
            Assert.Equal(user.FullName, result.FullName);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.ProfileImageUrl, result.ProfileImageUrl);
            Assert.Equal(user.CreatedAt, result.CreatedAt);

            _userRepositoryMock.Verify(
                repository => repository.GetByIdAsync(user.Id),
                Times.Once);
        }

        // ============================================================
        // UPDATE PROFILE
        // ============================================================

        [Fact]
        public async Task UpdateMeAsync_ShouldThrowArgumentException_WhenUserIdIsEmpty()
        {
            // Arrange
            var request = new UpdateUserRequest
            {
                FullName = "Updated User"
            };

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateMeAsync(
                    Guid.Empty,
                    request));

            // Assert
            Assert.Equal(
                "UserId cannot be empty.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateMeAsync_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var request = new UpdateUserRequest
            {
                FullName = "Updated User"
            };

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateMeAsync(
                    userId,
                    request));

            // Assert
            Assert.Equal(
                "User not found.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateMeAsync_ShouldUpdateFullNameAndSave()
        {
            // Arrange
            var user = CreateUser();

            var request = new UpdateUserRequest
            {
                FullName = "   Updated Name   "
            };

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            await _service.UpdateMeAsync(
                user.Id,
                request);

            // Assert
            Assert.Equal(
                "Updated Name",
                user.FullName);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task UpdateMeAsync_ShouldUpdateUsername_WhenUsernameIsAvailable()
        {
            // Arrange
            var user = CreateUser();

            var request = new UpdateUserRequest
            {
                Username = "   newusername   "
            };

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByUsernameAsync("newusername"))
                .ReturnsAsync((User?)null);

            // Act
            await _service.UpdateMeAsync(
                user.Id,
                request);

            // Assert
            Assert.Equal(
                "newusername",
                user.Username);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByUsernameAsync("newusername"),
                Times.Once);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task UpdateMeAsync_ShouldAllowCurrentUsername()
        {
            // Arrange
            var user = CreateUser(
                username: "diego");

            var request = new UpdateUserRequest
            {
                Username = "diego"
            };

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByUsernameAsync("diego"))
                .ReturnsAsync(user);

            // Act
            await _service.UpdateMeAsync(
                user.Id,
                request);

            // Assert
            Assert.Equal(
                "diego",
                user.Username);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task UpdateMeAsync_ShouldThrowInvalidOperationException_WhenUsernameIsAlreadyInUse()
        {
            // Arrange
            var user = CreateUser(
                username: "currentuser");

            var existingUser = CreateUser(
                username: "takenuser",
                email: "other@example.com");

            var request = new UpdateUserRequest
            {
                Username = "takenuser"
            };

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByUsernameAsync("takenuser"))
                .ReturnsAsync(existingUser);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.UpdateMeAsync(
                        user.Id,
                        request));

            // Assert
            Assert.Equal(
                "Username is already in use.",
                exception.Message);

            Assert.Equal(
                "currentuser",
                user.Username);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateMeAsync_ShouldChangePassword_WhenOldPasswordIsCorrect()
        {
            // Arrange
            const string oldPassword = "OldPassword123";
            const string newPassword = "NewPassword456";

            var user = CreateUser(
                passwordHash: HashPassword(oldPassword));

            var originalPasswordHash = user.PasswordHash;

            var request = new UpdateUserRequest
            {
                OldPassword = oldPassword,
                NewPassword = newPassword
            };

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            await _service.UpdateMeAsync(
                user.Id,
                request);

            // Assert
            Assert.NotEqual(
                originalPasswordHash,
                user.PasswordHash);

            Assert.Equal(
                HashPassword(newPassword),
                user.PasswordHash);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task UpdateMeAsync_ShouldThrowArgumentException_WhenOldPasswordIsIncorrect()
        {
            // Arrange
            const string correctPassword = "CorrectPassword";
            const string wrongPassword = "WrongPassword";

            var user = CreateUser(
                passwordHash: HashPassword(correctPassword));

            var originalPasswordHash = user.PasswordHash;

            var request = new UpdateUserRequest
            {
                OldPassword = wrongPassword,
                NewPassword = "NewPassword"
            };

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateMeAsync(
                    user.Id,
                    request));

            // Assert
            Assert.Equal(
                "Password does not match.",
                exception.Message);

            Assert.Equal(
                originalPasswordHash,
                user.PasswordHash);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateMeAsync_ShouldNotChangePassword_WhenOnlyNewPasswordIsProvided()
        {
            // Arrange
            var user = CreateUser(
                passwordHash: HashPassword("CurrentPassword"));

            var originalHash = user.PasswordHash;

            var request = new UpdateUserRequest
            {
                NewPassword = "NewPassword"
            };

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            await _service.UpdateMeAsync(
                user.Id,
                request);

            // Assert
            Assert.Equal(
                originalHash,
                user.PasswordHash);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task UpdateMeAsync_ShouldNotChangePassword_WhenOnlyOldPasswordIsProvided()
        {
            // Arrange
            var user = CreateUser(
                passwordHash: HashPassword("CurrentPassword"));

            var originalHash = user.PasswordHash;

            var request = new UpdateUserRequest
            {
                OldPassword = "CurrentPassword"
            };

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            await _service.UpdateMeAsync(
                user.Id,
                request);

            // Assert
            Assert.Equal(
                originalHash,
                user.PasswordHash);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task UpdateMeAsync_ShouldKeepCurrentValues_WhenFieldsAreBlank()
        {
            // Arrange
            var user = CreateUser(
                fullName: "Original Name",
                username: "original");

            var request = new UpdateUserRequest
            {
                FullName = "   ",
                Username = "   ",
                OldPassword = "   ",
                NewPassword = "   "
            };

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            await _service.UpdateMeAsync(
                user.Id,
                request);

            // Assert
            Assert.Equal(
                "Original Name",
                user.FullName);

            Assert.Equal(
                "original",
                user.Username);

            _userRepositoryMock.Verify(
                repository => repository.GetByUsernameAsync(
                    It.IsAny<string>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task UpdateMeAsync_ShouldUpdateFullNameUsernameAndPasswordTogether()
        {
            // Arrange
            const string oldPassword = "OldPassword";
            const string newPassword = "NewPassword";

            var user = CreateUser(
                fullName: "Old Name",
                username: "oldusername",
                passwordHash: HashPassword(oldPassword));

            var request = new UpdateUserRequest
            {
                FullName = "   New Name   ",
                Username = "   newusername   ",
                OldPassword = oldPassword,
                NewPassword = newPassword
            };

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByUsernameAsync("newusername"))
                .ReturnsAsync((User?)null);

            // Act
            await _service.UpdateMeAsync(
                user.Id,
                request);

            // Assert
            Assert.Equal(
                "New Name",
                user.FullName);

            Assert.Equal(
                "newusername",
                user.Username);

            Assert.Equal(
                HashPassword(newPassword),
                user.PasswordHash);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        // ============================================================
        // PROFILE IMAGE
        // ============================================================

        [Fact]
        public async Task UpdateProfileImageAsync_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();

            using var stream = new MemoryStream(
                new byte[] { 1, 2, 3 });

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateProfileImageAsync(
                    userId,
                    stream,
                    "profile.jpg",
                    "image/jpeg"));

            // Assert
            Assert.Equal(
                "User not found.",
                exception.Message);

            _fileStorageServiceMock.Verify(
                service => service.SaveUserProfileImageAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateProfileImageAsync_ShouldStoreImageUpdateUserAndSave()
        {
            // Arrange
            var user = CreateUser();

            const string imageUrl =
                "https://example.com/images/profile.jpg";

            using var stream = new MemoryStream(
                new byte[] { 1, 2, 3, 4 });

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _fileStorageServiceMock
                .Setup(service => service.SaveUserProfileImageAsync(
                    stream,
                    "profile.jpg",
                    "image/jpeg"))
                .ReturnsAsync(imageUrl);

            // Act
            var result = await _service.UpdateProfileImageAsync(
                user.Id,
                stream,
                "profile.jpg",
                "image/jpeg");

            // Assert
            Assert.Equal(
                imageUrl,
                result);

            Assert.Equal(
                imageUrl,
                user.ProfileImageUrl);

            _fileStorageServiceMock.Verify(
                service => service.SaveUserProfileImageAsync(
                    stream,
                    "profile.jpg",
                    "image/jpeg"),
                Times.Once);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task UpdateProfileImageAsync_ShouldTrimStoredImageUrlOnUser()
        {
            // Arrange
            var user = CreateUser();

            const string storedImageUrl =
                "   https://example.com/profile.png   ";

            using var stream = new MemoryStream(
                new byte[] { 1, 2, 3 });

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _fileStorageServiceMock
                .Setup(service => service.SaveUserProfileImageAsync(
                    stream,
                    "profile.png",
                    "image/png"))
                .ReturnsAsync(storedImageUrl);

            // Act
            var result = await _service.UpdateProfileImageAsync(
                user.Id,
                stream,
                "profile.png",
                "image/png");

            // Assert
            Assert.Equal(
                storedImageUrl,
                result);

            Assert.Equal(
                "https://example.com/profile.png",
                user.ProfileImageUrl);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task UpdateProfileImageAsync_ShouldNotSaveUser_WhenFileStorageFails()
        {
            // Arrange
            var user = CreateUser();

            using var stream = new MemoryStream(
                new byte[] { 1, 2, 3 });

            _userRepositoryMock
                .Setup(repository => repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _fileStorageServiceMock
                .Setup(service => service.SaveUserProfileImageAsync(
                    stream,
                    "profile.jpg",
                    "image/jpeg"))
                .ThrowsAsync(
                    new InvalidOperationException(
                        "Storage failure."));

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.UpdateProfileImageAsync(
                        user.Id,
                        stream,
                        "profile.jpg",
                        "image/jpeg"));

            // Assert
            Assert.Equal(
                "Storage failure.",
                exception.Message);

            Assert.Null(
                user.ProfileImageUrl);

            _userRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        /// <summary>
        /// Creates a valid user for unit tests.
        /// </summary>
        private static User CreateUser(
            string fullName = "Test User",
            string username = "testuser",
            string email = "test@example.com",
            string passwordHash = "password-hash")
        {
            return new User(
                fullName,
                username,
                email,
                passwordHash);
        }

        /// <summary>
        /// Creates the SHA-256 hash used by the current UserService
        /// password update workflow.
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