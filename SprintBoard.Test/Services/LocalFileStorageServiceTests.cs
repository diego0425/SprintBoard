using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using SprintBoard.api.Services;
using Xunit;

namespace SprintBoard.Test.Services
{
    /// <summary>
    /// Contains unit tests for local profile image persistence
    /// and public URL generation.
    /// </summary>
    public sealed class LocalFileStorageServiceTests
        : IDisposable
    {
        private const string PublicBaseUrl =
            "http://localhost:3000";

        private readonly string _webRootPath;

        /// <summary>
        /// Creates an isolated temporary web root used by each
        /// test execution so no real application files are modified.
        /// </summary>
        public LocalFileStorageServiceTests()
        {
            _webRootPath =
                Path.Combine(
                    Path.GetTempPath(),
                    "SprintBoard.Tests",
                    Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(
                _webRootPath);
        }

        /// <summary>
        /// Verifies that an uploaded profile image is physically
        /// persisted and that its public URL is returned.
        /// </summary>
        [Fact]
        public async Task SaveUserProfileImageAsync_ShouldSaveFileAndReturnPublicUrl_WhenConfigurationIsValid()
        {
            // Arrange
            var service =
                CreateService(PublicBaseUrl);

            byte[] fileContent =
            [
                10,
                20,
                30,
                40,
                50
            ];

            using var fileStream =
                new MemoryStream(fileContent);

            // Act
            var result =
                await service.SaveUserProfileImageAsync(
                    fileStream,
                    "profile.jpg",
                    "image/jpeg");

            // Assert
            Assert.StartsWith(
                $"{PublicBaseUrl}/uploads/profiles/",
                result);

            Assert.EndsWith(".jpg", result, StringComparison.OrdinalIgnoreCase);

            var storedFilePath =
                GetStoredFilePath(result);

            Assert.True(
                File.Exists(storedFilePath));

            var storedFileContent =
                await File.ReadAllBytesAsync(
                    storedFilePath,
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                fileContent,
                storedFileContent);
        }

        /// <summary>
        /// Verifies that a trailing slash in the configured public
        /// base URL does not produce a duplicated slash in the
        /// generated file URL.
        /// </summary>
        [Fact]
        public async Task SaveUserProfileImageAsync_ShouldTrimTrailingSlash_FromPublicBaseUrl()
        {
            // Arrange
            var service =
                CreateService(
                    $"{PublicBaseUrl}/");

            using var fileStream =
                new MemoryStream(
                    [1, 2, 3]);

            // Act
            var result =
                await service.SaveUserProfileImageAsync(
                    fileStream,
                    "profile.png",
                    "image/png");

            // Assert
            Assert.StartsWith(
                $"{PublicBaseUrl}/uploads/profiles/",
                result);

            Assert.DoesNotContain(
                "3000//uploads",
                result);
        }

        /// <summary>
        /// Verifies that the extension from the original uploaded
        /// file is preserved in the generated stored file name.
        /// </summary>
        [Fact]
        public async Task SaveUserProfileImageAsync_ShouldPreserveFileExtension()
        {
            // Arrange
            var service =
                CreateService(PublicBaseUrl);

            using var fileStream =
                new MemoryStream(
                    [1, 2, 3]);

            // Act
            var result =
                await service.SaveUserProfileImageAsync(
                    fileStream,
                    "avatar.profile.webp",
                    "image/webp");

            // Assert
            Assert.EndsWith(".webp", result, StringComparison.OrdinalIgnoreCase);

            Assert.True(
                File.Exists(
                    GetStoredFilePath(result)));
        }

        /// <summary>
        /// Verifies that separate uploads generate unique stored
        /// file names even when their original names are identical.
        /// </summary>
        [Fact]
        public async Task SaveUserProfileImageAsync_ShouldGenerateUniqueFileNames_ForMultipleUploads()
        {
            // Arrange
            var service =
                CreateService(PublicBaseUrl);

            using var firstStream =
                new MemoryStream(
                    [1, 2, 3]);

            using var secondStream =
                new MemoryStream(
                    [4, 5, 6]);

            // Act
            var firstResult =
                await service.SaveUserProfileImageAsync(
                    firstStream,
                    "profile.png",
                    "image/png");

            var secondResult =
                await service.SaveUserProfileImageAsync(
                    secondStream,
                    "profile.png",
                    "image/png");

            // Assert
            Assert.NotEqual(
                firstResult,
                secondResult);

            Assert.True(
                File.Exists(
                    GetStoredFilePath(firstResult)));

            Assert.True(
                File.Exists(
                    GetStoredFilePath(secondResult)));
        }

        /// <summary>
        /// Verifies that the storage service rejects an upload
        /// when no public base URL has been configured.
        /// </summary>
        [Fact]
        public async Task SaveUserProfileImageAsync_ShouldThrowInvalidOperationException_WhenPublicBaseUrlIsMissing()
        {
            // Arrange
            var service =
                CreateService("   ");

            using var fileStream =
                new MemoryStream(
                    [1, 2, 3]);

            // Act
            var exception =
                await Assert.ThrowsAsync<
                    InvalidOperationException>(
                    () =>
                        service.SaveUserProfileImageAsync(
                            fileStream,
                            "profile.jpg",
                            "image/jpeg"));

            // Assert
            Assert.Equal(
                "File storage public base URL is missing.",
                exception.Message);
        }

        /// <summary>
        /// Verifies that an invalid storage configuration
        /// is rejected before any upload directory or file
        /// is created on disk.
        /// </summary>
        [Fact]
        public async Task SaveUserProfileImageAsync_ShouldNotCreateFiles_WhenPublicBaseUrlIsMissing()
        {
            // Arrange
            var service =
                CreateService("   ");

            using var fileStream =
                new MemoryStream(
                    [1, 2, 3]);

            var profilesDirectoryPath =
                Path.Combine(
                    _webRootPath,
                    "uploads",
                    "profiles");

            // Act
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    service.SaveUserProfileImageAsync(
                        fileStream,
                        "profile.jpg",
                        "image/jpeg"));

            // Assert
            Assert.False(
                Directory.Exists(
                    profilesDirectoryPath));
        }

        /// <summary>
        /// Creates the system under test using an isolated temporary
        /// web root and the supplied public storage URL.
        /// </summary>
        /// <param name="publicBaseUrl">
        /// Public URL used when generating persisted image links.
        /// </param>
        /// <returns>
        /// Configured local file storage service.
        /// </returns>
        private LocalFileStorageService CreateService(
            string publicBaseUrl)
        {
            var webHostEnvironmentMock =
                new Mock<IWebHostEnvironment>();

            webHostEnvironmentMock
                .SetupGet(
                    environment =>
                        environment.WebRootPath)
                .Returns(_webRootPath);

            var options =
                Options.Create(
                    new FileStorageOptions
                    {
                        PublicBaseUrl =
                            publicBaseUrl
                    });

            return new LocalFileStorageService(
                webHostEnvironmentMock.Object,
                options);
        }

        /// <summary>
        /// Converts a public file URL returned by the service into
        /// its expected physical location inside the temporary
        /// web root.
        /// </summary>
        /// <param name="publicUrl">
        /// Public URL returned by the storage service.
        /// </param>
        /// <returns>
        /// Physical path of the stored file.
        /// </returns>
        private string GetStoredFilePath(
            string publicUrl)
        {
            var uri =
                new Uri(publicUrl);

            var storedFileName =
                Path.GetFileName(
                    uri.LocalPath);

            return Path.Combine(
                _webRootPath,
                "uploads",
                "profiles",
                storedFileName);
        }

        /// <summary>
        /// Removes all temporary files created by the tests.
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(
                _webRootPath))
            {
                Directory.Delete(
                    _webRootPath,
                    recursive: true);
            }
        }
    }
}