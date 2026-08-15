using Microsoft.EntityFrameworkCore;
using SprintBoard.Application.Interfaces;
using SprintBoard.Domain.Entities;

namespace SprintBoard.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Provides persistence operations for card checklist tasks.
    /// </summary>
    public sealed class CardTaskRepository : ICardTaskRepository
    {
        private readonly SprintBoardDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="CardTaskRepository"/> class.
        /// </summary>
        /// <param name="dbContext">
        /// The SprintBoard Entity Framework Core context used to execute queries and track persistence changes.
        /// </param>
        public CardTaskRepository(SprintBoardDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Gets a card task by its identifier.
        /// </summary>
        /// <param name="taskId">
        /// The card task identifier.
        /// </param>
        /// <returns>
        /// The matching card task, or <see langword="null"/> when not found.
        /// </returns>
        public async Task<CardTask?> GetByIdAsync(Guid taskId)
            => await _dbContext.CardTasks
                .FirstOrDefaultAsync(cardTask => cardTask.Id == taskId);

        /// <summary>
        /// Gets all checklist tasks that belong to a card.
        /// </summary>
        /// <param name="cardId">
        /// The card identifier.
        /// </param>
        /// <returns>
        /// The checklist tasks ordered by position.
        /// </returns>
        public async Task<IEnumerable<CardTask>> GetByCardAsync(Guid cardId)
            => await _dbContext.CardTasks
                .Where(cardTask => cardTask.CardId == cardId)
                .OrderBy(cardTask => cardTask.Position)
                .ToListAsync();

        /// <summary>
        /// Gets the card identifier associated with a checklist task.
        /// </summary>
        /// <param name="taskId">
        /// The card task identifier.
        /// </param>
        /// <returns>
        /// The card identifier, or <see langword="null"/> when the task does not exist.
        /// </returns>
        public async Task<Guid?> GetCardIdByTaskIdAsync(Guid taskId)
            => await _dbContext.CardTasks
                .Where(cardTask => cardTask.Id == taskId)
                .Select(cardTask => (Guid?)cardTask.CardId)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Adds a card task to the current unit of work.
        /// </summary>
        /// <param name="task">
        /// The card task to add.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public async Task AddAsync(CardTask task)
            => await _dbContext.CardTasks.AddAsync(task);

        /// <summary>
        /// Removes a card task from the current unit of work.
        /// </summary>
        /// <param name="task">
        /// The card task to remove.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        public Task RemoveAsync(CardTask task)
        {
            _dbContext.CardTasks.Remove(task);
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
