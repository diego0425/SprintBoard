namespace SprintBoard.Application.DTOs.Board
{
    /// <summary>
    /// Represents a request to change a board member's role.
    /// </summary>
    public sealed class ChangeBoardMemberRoleRequest
    {
        /// <summary>
        /// Gets or initializes the identifier of the member whose role will be changed.
        /// </summary>
        public Guid MemberUserId { get; init; }
        /// <summary>
        /// Gets or initializes the numeric value of the new board role.
        /// </summary>
        public int NewRole { get; init; }
    }
}
