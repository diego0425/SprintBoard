namespace SprintBoard.Application.DTOs.Board
{
    /// <summary>
    /// Represents a request to invite a user to a board.
    /// </summary>
    public sealed class CreateBoardInvitationRequest
    {
        /// <summary>
        /// Gets or initializes the email address that will receive the board invitation.
        /// </summary>
        public string Email { get; init; } = string.Empty;
    }
}
