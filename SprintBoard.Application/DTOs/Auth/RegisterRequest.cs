using System;

namespace SprintBoard.Application.DTOs.Auth
{
    /// <summary>
    /// Represents the information required to register a new user account.
    /// </summary>
    public sealed class RegisterRequest
    {
        /// <summary>
        /// Gets or initializes the user's full name.
        /// </summary>
        public string FullName { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the requested username.
        /// </summary>
        public string Username { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the email address for the new account.
        /// </summary>
        public string Email { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the password for the new account.
        /// </summary>
        public string Password { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the password confirmation value.
        /// </summary>
        public string RepeatPassword { get; init; } = string.Empty;
    }
}
