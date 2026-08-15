using SprintBoard.Domain.Entities;

namespace SprintBoard.Application.Interfaces
{
    /// <summary>
    /// Defines persistence operations required by the application layer for board invitations.
    /// </summary>
    public interface IBoardInvitationRepository
    {
        /// <summary>
        /// Stages a board invitation for persistence.
        /// </summary>
        /// <param name="invitation">
        /// The board invitation to add.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task AddAsync(BoardInvitation invitation);
        /// <summary>
        /// Retrieves a board invitation by its token.
        /// </summary>
        /// <param name="token">
        /// The invitation token.
        /// </param>
        /// <returns>
        /// The matching invitation, or <see langword="null"/> when no invitation exists for the token.
        /// </returns>
        Task<BoardInvitation?> GetByTokenAsync(string token);
        /// <summary>
        /// Determines whether a non-expired pending invitation exists for an email address on a board.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board being checked.
        /// </param>
        /// <param name="email">
        /// The normalized email address to check.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a non-expired pending invitation exists; otherwise, <see langword="false"/>.
        /// </returns>
        Task<bool> ExistsPendingAsync(Guid boardId, string email);
        /// <summary>
        /// Persists all pending board invitation changes.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task SaveChangesAsync();
    }
}
