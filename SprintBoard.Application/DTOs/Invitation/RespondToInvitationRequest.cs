namespace SprintBoard.Application.DTOs.Invitation
{
    /// <summary>
    /// Represents a request to respond to a board invitation.
    /// </summary>
    public sealed class RespondToInvitationRequest
    {
        /// <summary>
        /// Gets or initializes the token that identifies the invitation.
        /// </summary>
        public string Token { get; init; } = string.Empty;
    }
}
