using SprintBoard.Application.DTOs.User;
using SprintBoard.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace SprintBoard.Application.Services
{
    /// <summary>
    /// Coordinates user profile retrieval, account updates, password changes, and profile image storage.
    /// </summary>
    public sealed class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IFileStorageService _fileStorageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserService"/> class.
        /// </summary>
        /// <param name="userRepository">
        /// Repository used to retrieve and persist user account data.
        /// </param>
        /// <param name="fileStorageService">
        /// Storage service used to save user profile images and return their persisted location.
        /// </param>
        public UserService(IUserRepository userRepository, IFileStorageService fileStorageService)
        {
            _userRepository = userRepository;
            _fileStorageService = fileStorageService;
        }

        /// <summary>
        /// Retrieves user profile data by identifier.
        /// </summary>
        /// <param name="userId">
        /// The identifier of the user to retrieve.
        /// </param>
        /// <returns>
        /// The matching user profile data.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the user identifier is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the user does not exist.
        /// </exception>
        public async Task<UserResponse> GetByIdAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty.");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user is null)
                throw new KeyNotFoundException("User not found.");

            return new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                ProfileImageUrl = user.ProfileImageUrl,
                CreatedAt = user.CreatedAt
            };
        }

        /// <summary>
        /// Updates editable profile fields and optionally changes the authenticated user's password.
        /// </summary>
        /// <param name="userId">
        /// The identifier of the authenticated user being updated.
        /// </param>
        /// <param name="request">
        /// The profile values and optional password change data.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the user identifier is empty or the current password is invalid.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the user does not exist.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested username is already in use.
        /// </exception>
        public async Task UpdateMeAsync(Guid userId, UpdateUserRequest request)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty.");

            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null)
                throw new KeyNotFoundException("User not found.");

            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.UpdateFullName(request.FullName);

            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                var normalizedUsername = request.Username.Trim();

                var existingUser = await _userRepository.GetByUsernameAsync(normalizedUsername);

                if (existingUser is not null && existingUser.Id != user.Id)
                    throw new InvalidOperationException("Username is already in use.");

                user.UpdateUsername(request.Username);
            }

            if (!string.IsNullOrWhiteSpace(request.NewPassword) && !string.IsNullOrWhiteSpace(request.OldPassword))
            {
                var currentPasswordHash = HashPassword(request.OldPassword);

                if (!string.Equals(user.PasswordHash, currentPasswordHash, StringComparison.Ordinal))
                    throw new ArgumentException("Password does not match.");

                var newPasswordHash = HashPassword(request.NewPassword);

                user.ChangePassword(newPasswordHash);
            }

            await _userRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Stores a new profile image, updates the user record, and returns the stored image location.
        /// </summary>
        /// <param name="userId">
        /// The identifier of the user whose profile image will be updated.
        /// </param>
        /// <param name="fileStream">
        /// The stream containing the uploaded image data.
        /// </param>
        /// <param name="fileName">
        /// The original uploaded file name.
        /// </param>
        /// <param name="contentType">
        /// The MIME content type of the uploaded image.
        /// </param>
        /// <returns>
        /// The URL or path returned by the configured file storage service.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the user does not exist.
        /// </exception>
        public async Task<string> UpdateProfileImageAsync(Guid userId, Stream fileStream, string fileName, string contentType)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null)
                throw new KeyNotFoundException("User not found.");

            var imageUrl = await _fileStorageService.SaveUserProfileImageAsync(fileStream, fileName, contentType);

            user.UpdateProfileImage(imageUrl);

            await _userRepository.SaveChangesAsync();

            return imageUrl;
        }

        /// <summary>
        /// Computes the SHA-256 hash used by the current password update workflow.
        /// </summary>
        /// <param name="password">
        /// The plain-text password to hash.
        /// </param>
        /// <returns>
        /// The hexadecimal representation of the password hash.
        /// </returns>
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(hashBytes);
        }
    }
}
