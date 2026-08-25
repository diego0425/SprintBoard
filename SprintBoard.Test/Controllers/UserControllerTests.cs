using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SprintBoard.api.Controllers;
using SprintBoard.api.Services;
using SprintBoard.Application.DTOs.User;
using SprintBoard.Application.Interfaces;
using SprintBoard.Application.Services;
using SprintBoard.Domain.Entities;
using Xunit;

namespace SprintBoard.Test.Controllers
{
    /// <summary>
    /// Contains tests for the <see cref="UsersController"/>.
    /// </summary>
    public class UsersControllerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IFileStorageService> _fileStorageServiceMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;

        private readonly UserService _userService;
        private readonly UsersController _controller;

        /// <summary>
        /// Initializes the mocked dependencies and controller instance
        /// used by the user controller tests.
        /// </summary>
        public UsersControllerTests()
        {
            _userRepositoryMock =
                new Mock<IUserRepository>();

            _fileStorageServiceMock =
                new Mock<IFileStorageService>();

            _currentUserServiceMock =
                new Mock<ICurrentUserService>();

            _userService = new UserService(
                _userRepositoryMock.Object,
                _fileStorageServiceMock.Object);

            _controller = new UsersController(
                _userService,
                _currentUserServiceMock.Object);
        }

        // ============================================================
        // GET ME
        // ============================================================

        /// <summary>
        /// Verifies that GetMe returns an HTTP 200 response containing
        /// the profile information of the authenticated user.
        /// </summary>
        [Fact]
        public async Task GetMe_ShouldReturnOkWithCurrentUserProfile()
        {
            // Arrange
            var user = CreateUser();

            user.UpdateProfileImage(
                "https://example.com/profile.jpg");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(user.Id);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.GetMe();

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(result);

            Assert.Equal(
                StatusCodes.Status200OK,
                okResult.StatusCode);

            Assert.NotNull(okResult.Value);

            Assert.Equal(
                user.Id,
                GetProperty<Guid>(
                    okResult.Value!,
                    "Id"));

            Assert.Equal(
                user.Username,
                GetProperty<string>(
                    okResult.Value!,
                    "Username"));

            Assert.Equal(
                user.FullName,
                GetProperty<string>(
                    okResult.Value!,
                    "FullName"));

            Assert.Equal(
                user.Email,
                GetProperty<string>(
                    okResult.Value!,
                    "Email"));

            Assert.Equal(
                user.ProfileImageUrl,
                GetProperty<string?>(
                    okResult.Value!,
                    "ProfileImageUrl"));
        }

        /// <summary>
        /// Verifies that GetMe retrieves the user identified by
        /// the current authenticated user's identifier.
        /// </summary>
        [Fact]
        public async Task GetMe_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var user = CreateUser();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(user.Id);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            await _controller.GetMe();

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByIdAsync(user.Id),
                Times.Once);
        }

        /// <summary>
        /// Verifies that GetMe propagates an UnauthorizedAccessException
        /// when the authenticated user identifier cannot be resolved.
        /// </summary>
        [Fact]
        public async Task GetMe_ShouldPropagateUnauthorizedAccessException_WhenCurrentUserCannotBeResolved()
        {
            // Arrange
            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Throws(
                    new UnauthorizedAccessException(
                        "User is not authenticated."));

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => _controller.GetMe());

            // Assert
            Assert.Equal(
                "User is not authenticated.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByIdAsync(
                        It.IsAny<Guid>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that GetMe propagates a KeyNotFoundException
        /// when the authenticated user no longer exists.
        /// </summary>
        [Fact]
        public async Task GetMe_ShouldPropagateKeyNotFoundException_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => _controller.GetMe());

            // Assert
            Assert.Equal(
                "User not found.",
                exception.Message);
        }

        // ============================================================
        // UPDATE PROFILE IMAGE
        // ============================================================

        /// <summary>
        /// Verifies that UpdateProfileImage returns HTTP 400
        /// when no image file is provided.
        /// </summary>
        [Fact]
        public async Task UpdateProfileImage_ShouldReturnBadRequest_WhenFileIsNull()
        {
            // Act
            var result =
                await _controller.UpdateProfileImage(null!);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(
                StatusCodes.Status400BadRequest,
                badRequest.StatusCode);

            Assert.Equal(
                "File is required.",
                badRequest.Value);

            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Never);

            _fileStorageServiceMock.Verify(
                service =>
                    service.SaveUserProfileImageAsync(
                        It.IsAny<Stream>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that UpdateProfileImage returns HTTP 400
        /// when the uploaded file contains no data.
        /// </summary>
        [Fact]
        public async Task UpdateProfileImage_ShouldReturnBadRequest_WhenFileIsEmpty()
        {
            // Arrange
            var file = CreateFormFile(
                Array.Empty<byte>(),
                "empty.jpg",
                "image/jpeg");

            // Act
            var result =
                await _controller.UpdateProfileImage(file);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(
                "File is required.",
                badRequest.Value);

            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Never);

            _fileStorageServiceMock.Verify(
                service =>
                    service.SaveUserProfileImageAsync(
                        It.IsAny<Stream>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that UpdateProfileImage returns HTTP 400
        /// when the uploaded file has an unsupported content type.
        /// </summary>
        [Fact]
        public async Task UpdateProfileImage_ShouldReturnBadRequest_WhenContentTypeIsInvalid()
        {
            // Arrange
            var file = CreateFormFile(
                new byte[] { 1, 2, 3 },
                "profile.gif",
                "image/gif");

            // Act
            var result =
                await _controller.UpdateProfileImage(file);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(
                StatusCodes.Status400BadRequest,
                badRequest.StatusCode);

            Assert.Equal(
                "Only JPG, PNG and WEBP images are allowed.",
                badRequest.Value);

            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Never);

            _fileStorageServiceMock.Verify(
                service =>
                    service.SaveUserProfileImageAsync(
                        It.IsAny<Stream>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that UpdateProfileImage stores a valid JPEG image,
        /// updates the user profile and returns its persisted URL.
        /// </summary>
        [Fact]
        public async Task UpdateProfileImage_ShouldReturnOkWithProfileImageUrl_WhenJpegIsValid()
        {
            // Arrange
            var user = CreateUser();

            const string imageUrl =
                "https://example.com/profile.jpg";

            var file = CreateFormFile(
                new byte[] { 1, 2, 3, 4 },
                "profile.jpg",
                "image/jpeg");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(user.Id);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _fileStorageServiceMock
                .Setup(service =>
                    service.SaveUserProfileImageAsync(
                        It.IsAny<Stream>(),
                        "profile.jpg",
                        "image/jpeg"))
                .ReturnsAsync(imageUrl);

            // Act
            var result =
                await _controller.UpdateProfileImage(file);

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(result);

            Assert.Equal(
                StatusCodes.Status200OK,
                okResult.StatusCode);

            Assert.Equal(
                imageUrl,
                GetProperty<string>(
                    okResult.Value!,
                    "profileImageUrl"));

            Assert.Equal(
                imageUrl,
                user.ProfileImageUrl);

            _fileStorageServiceMock.Verify(
                service =>
                    service.SaveUserProfileImageAsync(
                        It.IsAny<Stream>(),
                        "profile.jpg",
                        "image/jpeg"),
                Times.Once);

            _userRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that UpdateProfileImage accepts PNG images
        /// as a supported profile image format.
        /// </summary>
        [Fact]
        public async Task UpdateProfileImage_ShouldAcceptPngImage()
        {
            // Arrange
            var user = CreateUser();

            var file = CreateFormFile(
                new byte[] { 1, 2, 3 },
                "profile.png",
                "image/png");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(user.Id);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _fileStorageServiceMock
                .Setup(service =>
                    service.SaveUserProfileImageAsync(
                        It.IsAny<Stream>(),
                        "profile.png",
                        "image/png"))
                .ReturnsAsync(
                    "https://example.com/profile.png");

            // Act
            var result =
                await _controller.UpdateProfileImage(file);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _fileStorageServiceMock.Verify(
                service =>
                    service.SaveUserProfileImageAsync(
                        It.IsAny<Stream>(),
                        "profile.png",
                        "image/png"),
                Times.Once);
        }

        /// <summary>
        /// Verifies that UpdateProfileImage accepts WebP images
        /// as a supported profile image format.
        /// </summary>
        [Fact]
        public async Task UpdateProfileImage_ShouldAcceptWebpImage()
        {
            // Arrange
            var user = CreateUser();

            var file = CreateFormFile(
                new byte[] { 1, 2, 3 },
                "profile.webp",
                "image/webp");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(user.Id);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _fileStorageServiceMock
                .Setup(service =>
                    service.SaveUserProfileImageAsync(
                        It.IsAny<Stream>(),
                        "profile.webp",
                        "image/webp"))
                .ReturnsAsync(
                    "https://example.com/profile.webp");

            // Act
            var result =
                await _controller.UpdateProfileImage(file);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _fileStorageServiceMock.Verify(
                service =>
                    service.SaveUserProfileImageAsync(
                        It.IsAny<Stream>(),
                        "profile.webp",
                        "image/webp"),
                Times.Once);
        }

        /// <summary>
        /// Verifies that UpdateProfileImage propagates an
        /// UnauthorizedAccessException when no authenticated user is available.
        /// </summary>
        [Fact]
        public async Task UpdateProfileImage_ShouldPropagateUnauthorizedAccessException_WhenCurrentUserCannotBeResolved()
        {
            // Arrange
            var file = CreateFormFile(
                new byte[] { 1, 2, 3 },
                "profile.jpg",
                "image/jpeg");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Throws(
                    new UnauthorizedAccessException(
                        "User is not authenticated."));

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () =>
                        _controller.UpdateProfileImage(file));

            // Assert
            Assert.Equal(
                "User is not authenticated.",
                exception.Message);

            _fileStorageServiceMock.Verify(
                service =>
                    service.SaveUserProfileImageAsync(
                        It.IsAny<Stream>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that UpdateProfileImage propagates a
        /// KeyNotFoundException when the authenticated user does not exist.
        /// </summary>
        [Fact]
        public async Task UpdateProfileImage_ShouldPropagateKeyNotFoundException_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var file = CreateFormFile(
                new byte[] { 1, 2, 3 },
                "profile.jpg",
                "image/jpeg");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () =>
                        _controller.UpdateProfileImage(file));

            // Assert
            Assert.Equal(
                "User not found.",
                exception.Message);

            _fileStorageServiceMock.Verify(
                service =>
                    service.SaveUserProfileImageAsync(
                        It.IsAny<Stream>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // UPDATE ME
        // ============================================================

        /// <summary>
        /// Verifies that UpdateMe returns HTTP 204 when the
        /// authenticated user's profile is successfully updated.
        /// </summary>
        [Fact]
        public async Task UpdateMe_ShouldReturnNoContent_WhenRequestIsValid()
        {
            // Arrange
            var user = CreateUser();

            var request = new UpdateUserRequest
            {
                FullName = "Updated User"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(user.Id);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            var result =
                await _controller.UpdateMe(request);

            // Assert
            Assert.IsType<NoContentResult>(result);

            Assert.Equal(
                "Updated User",
                user.FullName);

            _userRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that UpdateMe uses the identifier of the currently
        /// authenticated user when updating profile information.
        /// </summary>
        [Fact]
        public async Task UpdateMe_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var user = CreateUser();

            var request = new UpdateUserRequest
            {
                FullName = "Updated Name"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(user.Id);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            await _controller.UpdateMe(request);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByIdAsync(user.Id),
                Times.Once);

            _userRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that UpdateMe propagates an UnauthorizedAccessException
        /// when the authenticated user identifier cannot be resolved.
        /// </summary>
        [Fact]
        public async Task UpdateMe_ShouldPropagateUnauthorizedAccessException_WhenCurrentUserCannotBeResolved()
        {
            // Arrange
            var request = new UpdateUserRequest
            {
                FullName = "Updated Name"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Throws(
                    new UnauthorizedAccessException(
                        "User is not authenticated."));

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => _controller.UpdateMe(request));

            // Assert
            Assert.Equal(
                "User is not authenticated.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByIdAsync(
                        It.IsAny<Guid>()),
                Times.Never);

            _userRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that UpdateMe propagates an InvalidOperationException
        /// when the requested username is already used by another user.
        /// </summary>
        [Fact]
        public async Task UpdateMe_ShouldPropagateInvalidOperationException_WhenUsernameIsAlreadyInUse()
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

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(user.Id);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByUsernameAsync("takenuser"))
                .ReturnsAsync(existingUser);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _controller.UpdateMe(request));

            // Assert
            Assert.Equal(
                "Username is already in use.",
                exception.Message);

            Assert.Equal(
                "currentuser",
                user.Username);

            _userRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        /// <summary>
        /// Creates a valid user for controller tests.
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
        /// Creates an in-memory form file for profile image tests.
        /// </summary>
        private static IFormFile CreateFormFile(
            byte[] content,
            string fileName,
            string contentType)
        {
            var stream =
                new MemoryStream(content);

            return new FormFile(
                stream,
                0,
                stream.Length,
                "file",
                fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        /// <summary>
        /// Reads a property from an anonymous response object
        /// returned by a controller action.
        /// </summary>
        private static T GetProperty<T>(
            object value,
            string propertyName)
        {
            var property = value
                .GetType()
                .GetProperty(propertyName);

            Assert.NotNull(property);

            return (T)property!.GetValue(value)!;
        }
    }
}