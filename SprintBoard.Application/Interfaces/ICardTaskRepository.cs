using SprintBoard.Domain.Entities;

namespace SprintBoard.Application.Interfaces
{
    /// <summary>
    /// Defines persistence operations required by the application layer for card checklist tasks.
    /// </summary>
    public interface ICardTaskRepository
    {
        /// <summary>
        /// Retrieves a checklist task by its identifier.
        /// </summary>
        /// <param name="taskId">
        /// The checklist task identifier.
        /// </param>
        /// <returns>
        /// The matching checklist task, or <see langword="null"/> when not found.
        /// </returns>
        Task<CardTask?> GetByIdAsync(Guid taskId);
        /// <summary>
        /// Retrieves all checklist tasks that belong to a card.
        /// </summary>
        /// <param name="cardId">
        /// The parent card identifier.
        /// </param>
        /// <returns>
        /// The checklist tasks associated with the card.
        /// </returns>
        Task<IEnumerable<CardTask>> GetByCardAsync(Guid cardId);
        /// <summary>
        /// Retrieves the parent card identifier for a checklist task.
        /// </summary>
        /// <param name="taskId">
        /// The checklist task identifier.
        /// </param>
        /// <returns>
        /// The parent card identifier, or <see langword="null"/> when the task does not exist.
        /// </returns>
        Task<Guid?> GetCardIdByTaskIdAsync(Guid taskId);
        /// <summary>
        /// Stages a checklist task for persistence.
        /// </summary>
        /// <param name="task">
        /// The checklist task to add.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task AddAsync(CardTask task);
        /// <summary>
        /// Stages a checklist task for removal.
        /// </summary>
        /// <param name="task">
        /// The checklist task to remove.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task RemoveAsync(CardTask task);
        /// <summary>
        /// Persists all pending checklist task changes.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task SaveChangesAsync();
    }
}
