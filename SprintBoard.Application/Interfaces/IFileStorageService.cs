namespace SprintBoard.Application.Interfaces
{
    /// <summary>
    /// Defines file storage operations required by the application layer.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Stores a user profile image and returns a location that can be persisted with the user.
        /// </summary>
        /// <param name="fileStream">
        /// The stream containing the image data.
        /// </param>
        /// <param name="fileName">
        /// The original file name used to identify the uploaded image.
        /// </param>
        /// <param name="contentType">
        /// The MIME content type of the uploaded image.
        /// </param>
        /// <returns>
        /// The stored image URL or path.
        /// </returns>
        Task<string> SaveUserProfileImageAsync(Stream fileStream, string fileName, string contentType);
    }
}
