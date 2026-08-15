namespace SprintBoard.Application.DTOs.Board
{
    /// <summary>
    /// Represents board invitation data returned by the application layer.
    /// </summary>
    public sealed class BoardInvitationResponse
    {
        /// <summary>
        /// Gets or initializes the invitation identifier.
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// Gets or initializes the identifier of the board associated with the invitation.
        /// </summary>
        public Guid BoardId { get; init; }
        /// <summary>
        /// Gets or initializes the invited email address.
        /// </summary>
        public string Email { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the token used to identify the invitation.
        /// </summary>
        public string Token { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the UTC date and time when the invitation expires.
        /// </summary>
        public DateTime ExpiresAt { get; init; }
        /// <summary>
        /// Gets or initializes the UTC date and time when the invitation was created.
        /// </summary>
        public DateTime CreatedAt { get; init; }
    }
}
