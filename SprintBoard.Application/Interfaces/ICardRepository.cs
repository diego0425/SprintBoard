using SprintBoard.Domain.Entities;

namespace SprintBoard.Application.Interfaces
{
    /// <summary>
    /// Defines persistence operations required by the application layer for cards.
    /// </summary>
    public interface ICardRepository
    {
        /// <summary>
        /// Retrieves a card by its identifier.
        /// </summary>
        /// <param name="cardId">
        /// The card identifier.
        /// </param>
        /// <returns>
        /// The matching card, or <see langword="null"/> when not found.
        /// </returns>
        Task<Card?> GetByIdAsync(Guid cardId);
        /// <summary>
        /// Retrieves all cards that belong to a board.
        /// </summary>
        /// <param name="boardId">
        /// The board identifier.
        /// </param>
        /// <returns>
        /// The cards associated with the board.
        /// </returns>
        Task<IEnumerable<Card>> GetByBoardAsync(Guid boardId);
        /// <summary>
        /// Retrieves the parent board identifier for a card.
        /// </summary>
        /// <param name="cardId">
        /// The card identifier.
        /// </param>
        /// <returns>
        /// The parent board identifier, or <see langword="null"/> when the card does not exist.
        /// </returns>
        Task<Guid?> GetBoardIdByCardIdAsync(Guid cardId);
        /// <summary>
        /// Stages a card for persistence.
        /// </summary>
        /// <param name="card">
        /// The card to add.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task AddAsync(Card card);
        /// <summary>
        /// Stages a card for removal.
        /// </summary>
        /// <param name="card">
        /// The card to remove.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task RemoveAsync(Card card);
        /// <summary>
        /// Persists all pending card changes.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task SaveChangesAsync();
    }
}
