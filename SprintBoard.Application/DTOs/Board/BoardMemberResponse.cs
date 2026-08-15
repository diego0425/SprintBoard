using SprintBoard.Domain.Enums;
using System;

namespace SprintBoard.Application.DTOs.Board
{
    /// <summary>
    /// Represents a board member returned to an application client.
    /// </summary>
    public sealed class BoardMemberResponse
    {
        /// <summary>
        /// Gets or initializes the identifier of the member user.
        /// </summary>
        public Guid UserId { get; init; }
        /// <summary>
        /// Gets or initializes the username of the board member.
        /// </summary>
        public string Username { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the member's role within the board.
        /// </summary>
        public BoardRole Role { get; init; }
        /// <summary>
        /// Gets or initializes the profile picture of the board mermber.
        /// </summary>
        public string? ProfileImageUrl { get; init; }
    }
}
