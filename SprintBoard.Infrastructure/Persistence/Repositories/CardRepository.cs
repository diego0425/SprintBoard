using Microsoft.EntityFrameworkCore;
using SprintBoard.Application.Interfaces;
using SprintBoard.Domain.Entities;

namespace SprintBoard.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Provides persistence operations for cards.
    /// </summary>
    public sealed class CardRepository : ICardRepository
    {
        private readonly SprintBoardDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="CardRepository"/> class.
        /// </summary>
        /// <param name="dbContext">
        /// The SprintBoard Entity Framework Core context used to execute queries and track persistence changes.
        /// </param>
        public CardRepository(SprintBoardDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Gets a card by its identifier.
        /// </summary>
        /// <param name="cardId">
        /// The card identifier.
        /// </param>
        /// <returns>
        /// The matching card, or <see langword="null"/> when not found.
        /// </returns>
        public async Task<Card?> GetByIdAsync(Guid cardId)
            => await _dbContext.Cards
                .FirstOrDefaultAsync(card => card.Id == cardId);

        /// <summary>
        /// Gets the cards that belong to a board.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <returns>
        /// The board cards ordered by status and position.
        /// </returns>
        public async Task<IEnumerable<Card>> GetByBoardAsync(Guid boardId)
            => await _dbContext.Cards
                .Where(card => card.BoardId == boardId)
                .OrderBy(card => card.Status)
                .ThenBy(card => card.Position)
                .ToListAsync();

        /// <summary>
        /// Gets the board identifier associated with a card.
        /// </summary>
        /// <param name="cardId">
        /// The card identifier.
        /// </param>
        /// <returns>
        /// The board identifier, or <see langword="null"/> when the card does not exist.
        /// </returns>
        public async Task<Guid?> GetBoardIdByCardIdAsync(Guid cardId)
            => await _dbContext.Cards
                .Where(card => card.Id == cardId)
                .Select(card => (Guid?)card.BoardId)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Adds a card to the current unit of work.
        /// </summary>
        /// <param name="card">
        /// The card to add.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public async Task AddAsync(Card card)
            => await _dbContext.Cards.AddAsync(card);

        /// <summary>
        /// Removes a card from the current unit of work.
        /// </summary>
        /// <param name="card">
        /// The card to remove.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public Task RemoveAsync(Card card)
        {
            _dbContext.Cards.Remove(card);
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
