using Microsoft.EntityFrameworkCore;
using SprintBoard.Application.Interfaces;
using SprintBoard.Domain.Entities;
using SprintBoard.Domain.Enums;

namespace SprintBoard.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Provides persistence operations for board invitations.
    /// </summary>
    public sealed class BoardInvitationRepository : IBoardInvitationRepository
    {
        private readonly SprintBoardDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoardInvitationRepository"/> class.
        /// </summary>
        /// <param name="dbContext">
        /// The SprintBoard Entity Framework Core context used to execute queries and track persistence changes.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public BoardInvitationRepository(SprintBoardDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Adds a board invitation to the current unit of work.
        /// </summary>
        /// <param name="invitation">
        /// The invitation to add.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public async Task AddAsync(BoardInvitation invitation)
            => await _dbContext.BoardInvitations.AddAsync(invitation);

        /// <summary>
        /// Gets a board invitation by its unique token.
        /// </summary>
        /// <param name="token">
        /// The invitation token.
        /// </param>
        /// <returns>
        /// The matching invitation, or <see langword="null"/> when not found.
        /// </returns>
        public async Task<BoardInvitation?> GetByTokenAsync(string token)
            => await _dbContext.BoardInvitations
                .FirstOrDefaultAsync(invitation => invitation.Token == token);

        /// <summary>
        /// Determines whether a pending invitation already exists for an email address and board.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <param name="email">
        /// The invited email address.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a pending invitation exists; otherwise, <see langword="false"/>.
        /// </returns>
        public async Task<bool> ExistsPendingAsync(Guid boardId, string email)
            => await _dbContext.BoardInvitations.AnyAsync(invitation =>
                invitation.BoardId == boardId &&
                invitation.Email == email &&
                invitation.Status == InvitationStatus.Pending);

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
