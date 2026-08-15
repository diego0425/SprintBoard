using SprintBoard.Application.DTOs.CardTask;
using SprintBoard.Application.Interfaces;
using SprintBoard.Domain.Entities;

namespace SprintBoard.Application.Services
{
    /// <summary>
    /// Coordinates checklist task operations for cards while enforcing board membership.
    /// </summary>
    public class CardTaskService
    {
        private readonly ICardTaskRepository _cardTaskRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IMembershipAuthorizationService _membershipAuthorizationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CardTaskService"/> class.
        /// </summary>
        /// <param name="cardTaskRepository">
        /// Repository used to query and persist checklist tasks.
        /// </param>
        /// <param name="cardRepository">
        /// Repository used to validate and retrieve parent cards.
        /// </param>
        /// <param name="membershipAuthorizationService">
        /// Authorization service used to verify access through the card's parent board.
        /// </param>
        public CardTaskService(
            ICardTaskRepository cardTaskRepository,
            ICardRepository cardRepository,
            IMembershipAuthorizationService membershipAuthorizationService)
        {
            _cardTaskRepository = cardTaskRepository;
            _cardRepository = cardRepository;
            _membershipAuthorizationService = membershipAuthorizationService;
        }

        /// <summary>
        /// Creates a checklist task for a card after verifying board membership.
        /// </summary>
        /// <param name="cardId">
        /// The identifier of the card that will contain the checklist task.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user creating the task.
        /// </param>
        /// <param name="request">
        /// The checklist task title and optional position.
        /// </param>
        /// <returns>
        /// The newly created checklist task data.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the card identifier is empty or the title is missing.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the card does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the parent board.
        /// </exception>
        public async Task<CardTaskResponse> CreateAsync(Guid cardId, Guid userId, CreateCardTaskRequest request)
        {
            if (cardId == Guid.Empty)
                throw new ArgumentException("CardId cannot be empty.");

            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Title cannot be empty.");

            var card = await _cardRepository.GetByIdAsync(cardId);

            if (card is null)
                throw new KeyNotFoundException("Card not found.");

            await _membershipAuthorizationService.EnsureCardBoardMemberAsync(cardId, userId);

            var cardTask = new CardTask(cardId, request.Title, request.Position ?? 0);

            await _cardTaskRepository.AddAsync(cardTask);
            await _cardTaskRepository.SaveChangesAsync();

            return ToResponse(cardTask);
        }

        /// <summary>
        /// Retrieves checklist tasks for a card after verifying board membership.
        /// </summary>
        /// <param name="cardId">
        /// The identifier of the card whose checklist tasks will be retrieved.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user requesting the tasks.
        /// </param>
        /// <returns>
        /// The checklist tasks associated with the card.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the card identifier is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the card does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the parent board.
        /// </exception>
        public async Task<IEnumerable<CardTaskResponse>> GetByCardAsync(Guid cardId, Guid userId)
        {
            if (cardId == Guid.Empty)
                throw new ArgumentException("CardId cannot be empty.");

            var card = await _cardRepository.GetByIdAsync(cardId);

            if (card is null)
                throw new KeyNotFoundException("Card not found.");

            await _membershipAuthorizationService.EnsureCardBoardMemberAsync(cardId, userId);

            var cardTasks = await _cardTaskRepository.GetByCardAsync(cardId);

            return cardTasks.Select(ToResponse);
        }

        /// <summary>
        /// Marks a checklist task as completed after verifying access to the parent board.
        /// </summary>
        /// <param name="taskId">
        /// The identifier of the checklist task.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user requesting the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the checklist task identifier is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the checklist task does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the parent board.
        /// </exception>
        public async Task MarkAsCompletedAsync(Guid taskId, Guid userId)
        {
            if (taskId == Guid.Empty)
                throw new ArgumentException("TaskId cannot be empty.");

            var task = await _cardTaskRepository.GetByIdAsync(taskId);

            if (task is null)
                throw new KeyNotFoundException("Task not found.");

            await _membershipAuthorizationService.EnsureCardTaskBoardMemberAsync(taskId, userId);

            task.MarkAsCompleted();

            await _cardTaskRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Marks a checklist task as pending after verifying access to the parent board.
        /// </summary>
        /// <param name="taskId">
        /// The identifier of the checklist task.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user requesting the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the checklist task identifier is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the checklist task does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the parent board.
        /// </exception>
        public async Task MarkAsPendingAsync(Guid taskId, Guid userId)
        {
            if (taskId == Guid.Empty)
                throw new ArgumentException("TaskId cannot be empty.");

            var task = await _cardTaskRepository.GetByIdAsync(taskId);

            if (task is null)
                throw new KeyNotFoundException("Task not found.");

            await _membershipAuthorizationService.EnsureCardTaskBoardMemberAsync(taskId, userId);

            task.MarkAsPending();

            await _cardTaskRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Removes a checklist task after verifying access to the parent board.
        /// </summary>
        /// <param name="taskId">
        /// The identifier of the checklist task.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user requesting the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the checklist task identifier is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the checklist task does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the parent board.
        /// </exception>
        public async Task RemoveAsync(Guid taskId, Guid userId)
        {
            if (taskId == Guid.Empty)
                throw new ArgumentException("TaskId cannot be empty.");

            var task = await _cardTaskRepository.GetByIdAsync(taskId);

            if (task is null)
                throw new KeyNotFoundException("Task not found.");

            await _membershipAuthorizationService.EnsureCardTaskBoardMemberAsync(taskId, userId);

            await _cardTaskRepository.RemoveAsync(task);

            await _cardTaskRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Updates a checklist task after verifying access to the parent board.
        /// </summary>
        /// <param name="taskId">
        /// The identifier of the checklist task to update.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user requesting the update.
        /// </param>
        /// <param name="request">
        /// The checklist task values to update.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the checklist task identifier is empty or a supplied title is invalid.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the checklist task does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the parent board.
        /// </exception>
        public async Task UpdateAsync(Guid taskId, Guid userId, UpdateCardTaskRequest request)
        {
            if (taskId == Guid.Empty)
                throw new ArgumentException("TaskId cannot be empty.");

            var task = await _cardTaskRepository.GetByIdAsync(taskId);

            if (task is null)
                throw new KeyNotFoundException("Task not found.");

            await _membershipAuthorizationService.EnsureCardTaskBoardMemberAsync(taskId, userId);

            if (!string.IsNullOrWhiteSpace(request.Title))
                task.UpdateTitle(request.Title);

            await _cardTaskRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Maps a checklist task domain entity to its application response model.
        /// </summary>
        /// <param name="task">
        /// The checklist task entity to map.
        /// </param>
        /// <returns>
        /// A response containing the task data exposed by the application layer.
        /// </returns>
        private static CardTaskResponse ToResponse(CardTask task)
        {
            return new CardTaskResponse
            {
                Id = task.Id,
                CardId = task.CardId,
                Title = task.Title,
                IsCompleted = task.IsCompleted,
                Position = task.Position,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt
            };
        }
    }
}
