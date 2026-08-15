using Microsoft.EntityFrameworkCore;
using SprintBoard.Application.Interfaces;
using SprintBoard.Domain.Entities;
using SprintBoard.Domain.Enums;

namespace SprintBoard.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Provides persistence operations for board memberships.
    /// </summary>
    public sealed class BoardMemberRepository : IBoardMemberRepository
    {
        private readonly SprintBoardDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoardMemberRepository"/> class.
        /// </summary>
        /// <param name="dbContext">
        /// The SprintBoard Entity Framework Core context used to execute queries and track persistence changes.
        /// </param>
        public BoardMemberRepository(SprintBoardDbContext dbContext)
        {
            _dbContext = dbContext;
        }

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
        /// <see langword="true"/> when the membership exists; otherwise, <see langword="false"/>.
        /// </returns>
        public async Task<bool> ExistsAsync(Guid boardId, Guid userId)
            => await _dbContext.BoardMembers.AnyAsync(member =>
                member.BoardId == boardId &&
                member.UserId == userId);

        /// <summary>
        /// Gets a board membership by board and user identifiers.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <param name="userId">
        /// The user identifier.
        /// </param>
        /// <returns>
        /// The matching membership, or <see langword="null"/> when not found.
        /// </returns>
        public async Task<BoardMember?> GetMemberAsync(Guid boardId, Guid userId)
            => await _dbContext.BoardMembers.FirstOrDefaultAsync(member =>
                member.BoardId == boardId &&
                member.UserId == userId);

        /// <summary>
        /// Gets all members of a board, including their user information.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <returns>
        /// The board members ordered by role and username.
        /// </returns>
        public async Task<IEnumerable<BoardMember>> GetMembersAsync(Guid boardId)
            => await _dbContext.BoardMembers
                .Include(member => member.User)
                .Where(member => member.BoardId == boardId)
                .OrderByDescending(member => member.Role)
                .ThenBy(member => member.User.Username)
                .ToListAsync();

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
        /// <see langword="true"/> when the user is the owner; otherwise, <see langword="false"/>.
        /// </returns>
        public async Task<bool> IsOwnerAsync(Guid boardId, Guid userId)
            => await HasRoleAsync(boardId, userId, BoardRole.Owner);

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
        /// <see langword="true"/> when the user is an administrator; otherwise, <see langword="false"/>.
        /// </returns>
        public async Task<bool> IsAdminAsync(Guid boardId, Guid userId)
            => await HasRoleAsync(boardId, userId, BoardRole.Admin);

        /// <summary>
        /// Determines whether a user is the owner or an administrator of a board.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <param name="userId">
        /// The user identifier.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the user has elevated board permissions; otherwise, <see langword="false"/>.
        /// </returns>
        public async Task<bool> IsOwnerOrAdminAsync(Guid boardId, Guid userId)
            => await _dbContext.BoardMembers.AnyAsync(member =>
                member.BoardId == boardId &&
                member.UserId == userId &&
                (member.Role == BoardRole.Owner || member.Role == BoardRole.Admin));

        /// <summary>
        /// Gets a board membership by board and user identifiers.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <param name="userId">
        /// The user identifier.
        /// </param>
        /// <returns>
        /// The matching membership, or <see langword="null"/> when not found.
        /// </returns>
        public async Task<BoardMember?> GetAsync(Guid boardId, Guid userId)
            => await GetMemberAsync(boardId, userId);

        /// <summary>
        /// Adds a board membership to the current unit of work.
        /// </summary>
        /// <param name="boardMember">
        /// The board membership to add.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public async Task AddAsync(BoardMember boardMember)
            => await _dbContext.BoardMembers.AddAsync(boardMember);

        /// <summary>
        /// Removes a board membership from the current unit of work.
        /// </summary>
        /// <param name="boardMember">
        /// The board membership to remove.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public Task RemoveAsync(BoardMember boardMember)
        {
            _dbContext.BoardMembers.Remove(boardMember);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Persists all pending changes to the database.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public async Task SaveChangesAsync()
            => await _dbContext.SaveChangesAsync();

        /// <summary>
        /// Determines whether a board membership exists with a specific role.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <param name="userId">
        /// The user identifier.
        /// </param>
        /// <param name="role">
        /// The role that the membership must have.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the user has the requested role on the board; otherwise, <see langword="false"/>.
        /// </returns>
        private async Task<bool> HasRoleAsync(Guid boardId, Guid userId, BoardRole role)
            => await _dbContext.BoardMembers.AnyAsync(member =>
                member.BoardId == boardId &&
                member.UserId == userId &&
                member.Role == role);
    }
}
