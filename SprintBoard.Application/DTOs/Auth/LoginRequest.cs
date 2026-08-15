using System;

namespace SprintBoard.Application.DTOs.Auth
{
    /// <summary>
    /// Represents the credentials required to authenticate a user.
    /// </summary>
    public sealed class LoginRequest
    {
        /// <summary>
        /// Gets or initializes the email address used to identify the account.
        /// </summary>
        public string Email { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the plain-text password supplied for authentication.
        /// </summary>
        public string Password { get; init; } = string.Empty;
    }

}
