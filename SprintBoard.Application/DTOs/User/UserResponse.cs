namespace SprintBoard.Application.DTOs.User
{
    /// <summary>
    /// Represents user profile data returned by the application layer.
    /// </summary>
    public sealed class UserResponse
    {
        /// <summary>
        /// Gets or initializes the user identifier.
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// Gets or initializes the user's username.
        /// </summary>
        public string Username { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the user's full name.
        /// </summary>
        public string FullName { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the user's email address.
        /// </summary>
        public string Email { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the optional URL of the user's profile image.
        /// </summary>
        public string? ProfileImageUrl { get; init; }
        /// <summary>
        /// Gets or initializes the UTC date and time when the user account was created.
        /// </summary>
        public DateTime CreatedAt { get; init; }
    }
}
