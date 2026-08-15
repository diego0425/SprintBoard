using Microsoft.EntityFrameworkCore;
using SprintBoard.Application.Interfaces;
using SprintBoard.Domain.Entities;

namespace SprintBoard.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Provides persistence operations for boards.
    /// </summary>
    public sealed class BoardRepository : IBoardRepository
    {
        private readonly SprintBoardDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoardRepository"/> class.
        /// </summary>
        /// <param name="dbContext">
        /// The SprintBoard Entity Framework Core context used to execute queries and track persistence changes.
        /// </param>
        public BoardRepository(SprintBoardDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Gets a board by its identifier.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <returns>
        /// The matching board, or <see langword="null"/> when not found.
        /// </returns>
        public async Task<Board?> GetByIdAsync(Guid boardId)
            => await _dbContext.Boards
                .FirstOrDefaultAsync(board => board.Id == boardId);

        /// <summary>
        /// Gets all boards owned by a user.
        /// </summary>
        /// <param name="ownerId">
        /// The owner identifier.
        /// </param>
        /// <returns>
        /// The boards owned by the specified user.
        /// </returns>
        public async Task<IEnumerable<Board>> GetByOwnerAsync(Guid ownerId)
            => await _dbContext.Boards
                .Where(board => board.OwnerId == ownerId)
                .ToListAsync();

        /// <summary>
        /// Gets all boards in which a user has a membership.
        /// </summary>
        /// <param name="userId">
        /// The user identifier.
        /// </param>
        /// <returns>
        /// The boards available to the specified user.
        /// </returns>
        public async Task<IEnumerable<Board>> GetByUserMembershipAsync(Guid userId)
            => await _dbContext.Boards
                .Where(board => board.Members.Any(member => member.UserId == userId))
                .ToListAsync();

        /// <summary>
        /// Adds a board to the current unit of work.
        /// </summary>
        /// <param name="board">
        /// The board to add.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public async Task AddAsync(Board board)
            => await _dbContext.Boards.AddAsync(board);

        /// <summary>
        /// Removes a board from the current unit of work.
        /// </summary>
        /// <param name="board">
        /// The board to remove.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public Task RemoveAsync(Board board)
        {
            _dbContext.Boards.Remove(board);
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
    }
}
