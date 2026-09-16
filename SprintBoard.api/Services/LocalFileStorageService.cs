using Microsoft.Extensions.Options;
using SprintBoard.Application.Interfaces;

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
    /// Saves a user profile image to local storage.
    /// </summary>
    /// <param name="fileStream">
    /// Stream containing the image data.
    /// </param>
    /// <param name="fileName">
    /// Original file name used to preserve the
    /// uploaded file extension.
    /// </param>
    /// <param name="contentType">
    /// MIME type of the uploaded file.
    /// </param>
    /// <returns>
    /// Public URL that can be used to access the
    /// stored profile image.
    /// </returns>
    public async Task<string>
        SaveUserProfileImageAsync(
            Stream fileStream,
            string fileName,
            string contentType)
    {
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

        var fileExtension =
            Path.GetExtension(fileName);

        var storedFileName =
            $"{Guid.NewGuid()}{fileExtension}";

        var storedFilePath =
            Path.Combine(
                profilesDirectoryPath,
                storedFileName);

        await using (
            var outputStream =
                new FileStream(
                    storedFilePath,
                    FileMode.Create))
        {
            await fileStream.CopyToAsync(
                outputStream);
        }

        var publicBaseUrl =
            _options.PublicBaseUrl
                .TrimEnd('/');

        if (string.IsNullOrWhiteSpace(
            publicBaseUrl))
        {
            throw new InvalidOperationException(
                "File storage public base URL is missing.");
        }

        return
            $"{publicBaseUrl}/" +
            $"{UploadsFolderName}/" +
            $"{ProfilesFolderName}/" +
            $"{storedFileName}";
    }
}