using Microsoft.Extensions.Options;
using SprintBoard.Application.Interfaces;
using SprintBoard.api.Security;

namespace SprintBoard.api.Services;

/// <summary>
/// Stores uploaded user profile images in the API web root
/// and returns their public URL.
/// </summary>
public sealed class LocalFileStorageService
    : IFileStorageService
{
    private const string ProfilesFolderName =
        "profiles";

    private const string UploadsFolderName =
        "uploads";

    private readonly IWebHostEnvironment
        _webHostEnvironment;

    private readonly FileStorageOptions
        _options;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="LocalFileStorageService"/> class.
    /// </summary>
    /// <param name="webHostEnvironment">
    /// Hosting environment used to resolve the
    /// application's web root directory.
    /// </param>
    /// <param name="options">
    /// Storage configuration containing the public
    /// base URL used when generating file URLs.
    /// </param>
    public LocalFileStorageService(
        IWebHostEnvironment webHostEnvironment,
        IOptions<FileStorageOptions> options)
    {
        _webHostEnvironment =
            webHostEnvironment;

        _options =
            options.Value;
    }

    /// <summary>
    /// Validates and saves a user profile image to local storage.
    /// </summary>
    /// <param name="fileStream">
    /// Stream containing the uploaded image data.
    /// </param>
    /// <param name="fileName">
    /// Original uploaded file name used only during
    /// security validation.
    /// </param>
    /// <param name="contentType">
    /// MIME type declared for the uploaded image.
    /// </param>
    /// <returns>
    /// Public URL of the safely stored profile image.
    /// </returns>
    public async Task<string>
        SaveUserProfileImageAsync(
            Stream fileStream,
            string fileName,
            string contentType)
    {
        var publicBaseUrl =
            _options.PublicBaseUrl
                .TrimEnd('/');

        if (string.IsNullOrWhiteSpace(
                publicBaseUrl))
        {
            throw new InvalidOperationException(
                "File storage public base URL is missing.");
        }

        var validatedImage =
            await ProfileImageContentValidator
                .ValidateAsync(
                    fileStream,
                    fileName,
                    contentType);

        var webRootPath =
            _webHostEnvironment.WebRootPath
            ?? "wwwroot";

        var profilesDirectoryPath =
            Path.Combine(
                webRootPath,
                UploadsFolderName,
                ProfilesFolderName);

        Directory.CreateDirectory(
            profilesDirectoryPath);

        /*
         * Never persist the extension supplied directly
         * by the client. The validator provides a trusted
         * canonical extension derived from the validated
         * image format.
         */
        var storedFileName =
            $"{Guid.NewGuid():N}" +
            validatedImage.FileExtension;

        var storedFilePath =
            Path.Combine(
                profilesDirectoryPath,
                storedFileName);

        await File.WriteAllBytesAsync(
            storedFilePath,
            validatedImage.Content);

        return
            $"{publicBaseUrl}/" +
            $"{UploadsFolderName}/" +
            $"{ProfilesFolderName}/" +
            $"{storedFileName}";
    }
}