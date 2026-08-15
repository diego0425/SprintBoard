namespace SprintBoard.Application.DTOs.User
{
    /// <summary>
    /// Represents the editable values of the authenticated user profile.
    /// </summary>
    public sealed class UpdateUserRequest
    {
        /// <summary>
        /// Gets or initializes the user's new full name, or <see langword="null"/> to leave it unchanged.
        /// </summary>
        public string? FullName { get; init; }
        /// <summary>
        /// Gets or initializes the new username, or <see langword="null"/> to leave it unchanged.
        /// </summary>
        public string? Username { get; init; }
        /// <summary>
        /// Gets or initializes the current password required when changing the account password.
        /// </summary>
        public string? OldPassword { get; init; }
        /// <summary>
        /// Gets or initializes the replacement password.
        /// </summary>
        public string? NewPassword { get; init; }
    }
}
