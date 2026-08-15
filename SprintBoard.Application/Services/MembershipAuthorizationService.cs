using SprintBoard.Application.Exceptions;
using SprintBoard.Application.Interfaces;

namespace SprintBoard.Application.Services
{
    /// <summary>
    /// Enforces board authorization rules for boards, cards, and checklist tasks.
    /// </summary>
    public class MembershipAuthorizationService : IMembershipAuthorizationService
    {
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly ICardRepository _cardRepository;
        private readonly ICardTaskRepository _cardTaskRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="MembershipAuthorizationService"/> class.
        /// </summary>
        /// <param name="boardMemberRepository">
        /// Repository used to evaluate board memberships and roles.
        /// </param>
        /// <param name="cardRepository">
        /// Repository used to resolve the board that contains a card.
        /// </param>
        /// <param name="cardTaskRepository">
        /// Repository used to resolve the card that contains a checklist task.
        /// </param>
        public MembershipAuthorizationService(
            IBoardMemberRepository boardMemberRepository,
            ICardRepository cardRepository,
            ICardTaskRepository cardTaskRepository)
        {
            _boardMemberRepository = boardMemberRepository;
            _cardRepository = cardRepository;
            _cardTaskRepository = cardTaskRepository;
        }

        /// <summary>
        /// Ensures that a user is a member of a board.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board being checked.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user whose membership is being verified.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the board.
        /// </exception>
        public async Task EnsureBoardMemberAsync(Guid boardId, Guid userId)
        {
            var isMember = await _boardMemberRepository.ExistsAsync(boardId, userId);

            if (!isMember)
                throw new ForbiddenAccessException("You are not a member of this board.");
        }

        /// <summary>
        /// Ensures that a user owns a board.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board being checked.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user whose ownership is being verified.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not the board owner.
        /// </exception>
        public async Task EnsureBoardOwnerAsync(Guid boardId, Guid userId)
        {
            var isOwner = await _boardMemberRepository.IsOwnerAsync(boardId, userId);

            if (!isOwner)
                throw new ForbiddenAccessException("Only the board owner can perform this action.");
        }

        /// <summary>
        /// Ensures that a user has owner or administrator privileges on a board.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board being checked.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user whose role is being verified.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is neither the owner nor an administrator.
        /// </exception>
        public async Task EnsureBoardOwnerOrAdminAsync(Guid boardId, Guid userId)
        {
            var isOwnerOrAdmin = await _boardMemberRepository.IsOwnerOrAdminAsync(boardId, userId);

            if (!isOwnerOrAdmin)
                throw new ForbiddenAccessException("You do not have permission to perform this action.");
        }

        /// <summary>
        /// Ensures that a user belongs to the board containing a card.
        /// </summary>
        /// <param name="cardId">
        /// The identifier of the card used to resolve the parent board.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user whose board membership is being verified.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the card does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the parent board.
        /// </exception>
        public async Task EnsureCardBoardMemberAsync(Guid cardId, Guid userId)
        {
            var boardId = await _cardRepository.GetBoardIdByCardIdAsync(cardId);

            if (boardId is null)
                throw new KeyNotFoundException("Card not found.");

            await EnsureBoardMemberAsync(boardId.Value, userId);
        }

        /// <summary>
        /// Ensures that a user belongs to the board containing a checklist task.
        /// </summary>
        /// <param name="taskId">
        /// The identifier of the checklist task used to resolve its parent card and board.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user whose board membership is being verified.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the checklist task or parent card does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the parent board.
        /// </exception>
        public async Task EnsureCardTaskBoardMemberAsync(Guid taskId, Guid userId)
        {
            var cardId = await _cardTaskRepository.GetCardIdByTaskIdAsync(taskId);

            if (cardId is null)
                throw new KeyNotFoundException("Task not found.");

            await EnsureCardBoardMemberAsync(cardId.Value, userId);
        }
    }
}
