using SprintBoard.Domain.Entities;

namespace SprintBoard.Application.Interfaces
{
    /// <summary>
    /// Defines persistence operations required by the application layer for boards.
    /// </summary>
    public interface IBoardRepository
    {
        /// <summary>
        /// Retrieves a board by its identifier.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <returns>
        /// The matching board, or <see langword="null"/> when not found.
        /// </returns>
        Task<Board?> GetByIdAsync(Guid boardId);
        /// <summary>
        /// Retrieves boards in which a user has a membership.
        /// </summary>
        /// <param name="userId">
        /// The member user identifier.
        /// </param>
        /// <returns>
        /// The boards accessible through the user's memberships.
        /// </returns>
        Task<IEnumerable<Board>> GetByUserMembershipAsync(Guid userId);
        /// <summary>
        /// Stages a board for persistence.
        /// </summary>
        /// <param name="board">
        /// The board to add.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task AddAsync(Board board);
        /// <summary>
        /// Stages a board for removal.
        /// </summary>
        /// <param name="board">
        /// The board to remove.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task RemoveAsync(Board board);
        /// <summary>
        /// Persists all pending board changes.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task SaveChangesAsync();
    }
}
