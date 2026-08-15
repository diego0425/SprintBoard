using System;

namespace SprintBoard.Application.DTOs.Auth
{
    /// <summary>
    /// Represents authentication data returned after a successful sign-in.
    /// </summary>
    public sealed class AuthResponse
    {
        /// <summary>
        /// Gets or initializes the access token issued to the authenticated user.
        /// </summary>
        public string AccessToken { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the UTC date and time when the access token expires.
        /// </summary>
        public DateTime ExpiresAtUtc { get; init; }
    }
}
