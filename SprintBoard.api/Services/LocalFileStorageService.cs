using SprintBoard.Application.Interfaces;

namespace SprintBoard.api.Services;

/// <summary>
/// Stores uploaded user profile images in the API web root and returns their public URL.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private const string ProfilesFolderName = "profiles";
    private const string UploadsFolderName = "uploads";

    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileStorageService"/> class.
    /// </summary>
    /// <param name="webHostEnvironment">
    /// Hosting environment used to resolve the application's web root directory.
    /// </param>
    /// <param name="httpContextAccessor">
    /// Accessor used to build the public base URL from the current HTTP request.
    /// </param>
    public LocalFileStorageService(
        IWebHostEnvironment webHostEnvironment,
        IHttpContextAccessor httpContextAccessor)
    {
        _webHostEnvironment = webHostEnvironment;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Saves a user profile image to local storage.
    /// </summary>
    /// <param name="fileStream">
    /// Stream containing the file contents to be persisted.
    /// </param>
    /// <param name="fileName">
    /// Original file name used to preserve the uploaded file extension.
    /// </param>
    /// <param name="contentType">
    /// MIME type of the uploaded file.
    /// </param>
    /// <returns>
    /// Public URL that can be used to access the stored profile image.
    /// </returns>
    public async Task<string> SaveUserProfileImageAsync(
        Stream fileStream,
        string fileName,
        string contentType)
    {
        var webRootPath = _webHostEnvironment.WebRootPath ?? "wwwroot";
        var profilesDirectoryPath = Path.Combine(
            webRootPath,
            UploadsFolderName,
            ProfilesFolderName);

        Directory.CreateDirectory(profilesDirectoryPath);

        var fileExtension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid()}{fileExtension}";
        var storedFilePath = Path.Combine(profilesDirectoryPath, storedFileName);

        await using (var outputStream = new FileStream(storedFilePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(outputStream);
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        var apiBaseUrl = request is null
            ? string.Empty
            : $"{request.Scheme}://{request.Host}";

        return $"{apiBaseUrl}/{UploadsFolderName}/{ProfilesFolderName}/{storedFileName}";
    }
}
