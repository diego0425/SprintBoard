using SprintBoard.Domain.Entities;
using SprintBoard.Domain.Enums;

namespace SprintBoard.Application.Interfaces
{
    /// <summary>
    /// Defines persistence queries and commands for board memberships and roles.
    /// </summary>
    public interface IBoardMemberRepository
    {
        /// <summary>
        /// Determines whether a user belongs to a board.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <param name="userId">
        /// The user identifier.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the user is a member of the board; otherwise, <see langword="false"/>.
        /// </returns>
        Task<bool> ExistsAsync(Guid boardId, Guid userId);
        /// <summary>
        /// Retrieves a board membership for a specific user.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <param name="userId">
        /// The member user identifier.
        /// </param>
        /// <returns>
        /// The matching membership, or <see langword="null"/> when it does not exist.
        /// </returns>
        Task<BoardMember?> GetMemberAsync(Guid boardId, Guid userId);
        /// <summary>
        /// Retrieves all members of a board.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <returns>
        /// The memberships associated with the board.
        /// </returns>
        Task<IEnumerable<BoardMember>> GetMembersAsync(Guid boardId);
        /// <summary>
        /// Determines whether a user is the owner of a board.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <param name="userId">
        /// The user identifier.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the user owns the board; otherwise, <see langword="false"/>.
        /// </returns>
        Task<bool> IsOwnerAsync(Guid boardId, Guid userId);
        /// <summary>
        /// Determines whether a user is an administrator of a board.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <param name="userId">
        /// The user identifier.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the user is a board administrator; otherwise, <see langword="false"/>.
        /// </returns>
        Task<bool> IsAdminAsync(Guid boardId, Guid userId);
        /// <summary>
        /// Determines whether a user has owner or administrator privileges on a board.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <param name="userId">
        /// The user identifier.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the user is an owner or administrator; otherwise, <see langword="false"/>.
        /// </returns>
        Task<bool> IsOwnerOrAdminAsync(Guid boardId, Guid userId);
        /// <summary>
        /// Retrieves a board membership by board and user identifiers.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <param name="userId">
        /// The user identifier.
        /// </param>
        /// <returns>
        /// The matching membership, or <see langword="null"/> when it does not exist.
        /// </returns>
        Task<BoardMember?> GetAsync(Guid boardId, Guid userId);
        /// <summary>
        /// Stages a board membership for persistence.
        /// </summary>
        /// <param name="boardMember">
        /// The board membership to add.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task AddAsync(BoardMember boardMember);
        /// <summary>
        /// Stages a board membership for removal.
        /// </summary>
        /// <param name="boardMember">
        /// The board membership to remove.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task RemoveAsync(BoardMember boardMember);
        /// <summary>
        /// Persists all pending board membership changes.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task SaveChangesAsync();
    }
}
