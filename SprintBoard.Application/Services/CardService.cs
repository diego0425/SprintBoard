using SprintBoard.Application.DTOs.Card;
using SprintBoard.Application.Interfaces;
using SprintBoard.Domain.Entities;
using SprintBoard.Domain.Enums;

namespace SprintBoard.Application.Services
{
    /// <summary>
    /// Coordinates card creation, retrieval, status changes, updates, and removal within boards.
    /// </summary>
    public sealed class CardService
    {
        private readonly IBoardRepository _boardRepository;
        private readonly ICardRepository _cardRepository;
        private readonly IMembershipAuthorizationService _membershipAuthorizationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CardService"/> class.
        /// </summary>
        /// <param name="boardRepository">
        /// Repository used to validate and retrieve parent boards.
        /// </param>
        /// <param name="cardRepository">
        /// Repository used to query and persist cards.
        /// </param>
        /// <param name="membershipAuthorizationService">
        /// Authorization service used to ensure users can access cards through board membership.
        /// </param>
        public CardService(
            IBoardRepository boardRepository,
            ICardRepository cardRepository,
            IMembershipAuthorizationService membershipAuthorizationService)
        {
            _boardRepository = boardRepository;
            _cardRepository = cardRepository;
            _membershipAuthorizationService = membershipAuthorizationService;
        }

        /// <summary>
        /// Creates a card in a board after verifying that the user is a board member.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board that will contain the new card.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user creating the card.
        /// </param>
        /// <param name="request">
        /// The card title, description, and optional initial position.
        /// </param>
        /// <returns>
        /// The newly created card data.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the board identifier is empty or the card title is missing.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the board does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the board.
        /// </exception>
        public async Task<CardResponse> CreateAsync(Guid boardId, Guid userId, CreateCardRequest request)
        {
            if (boardId == Guid.Empty)
                throw new ArgumentException("BoardId cannot be empty.");

            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Title cannot be empty.");

            var board = await _boardRepository.GetByIdAsync(boardId);

            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            await _membershipAuthorizationService.EnsureBoardMemberAsync(boardId, userId);

            var position = request.Position ?? 0;
            var card = new Card(boardId, request.Title, request.Description, position);

            await _cardRepository.AddAsync(card);
            await _cardRepository.SaveChangesAsync();

            return new CardResponse
            {
                Id = card.Id,
                BoardId = card.BoardId,
                Title = card.Title,
                Description = card.Description,
                Status = card.Status,
                Position = card.Position,
                CreatedAt = card.CreatedAt,
                UpdatedAt = card.UpdatedAt
            };
        }

        /// <summary>
        /// Retrieves cards from a board after verifying requester membership.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board whose cards will be retrieved.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user requesting the cards.
        /// </param>
        /// <returns>
        /// The cards associated with the board, ordered by workflow status.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the board identifier is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the board does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the board.
        /// </exception>
        public async Task<IEnumerable<CardResponse>> GetByBoardAsync(Guid boardId, Guid userId)
        {
            if (boardId == Guid.Empty)
                throw new ArgumentException("BoardId cannot be empty.");

            var board = await _boardRepository.GetByIdAsync(boardId);

            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            await _membershipAuthorizationService.EnsureBoardMemberAsync(boardId, userId);

            var cards = await _cardRepository.GetByBoardAsync(boardId);

            return cards.Select(card => new CardResponse
            {
                Id = card.Id,
                BoardId = card.BoardId,
                Title = card.Title,
                Description = card.Description,
                Status = card.Status,
                Position = card.Position,
                CreatedAt = card.CreatedAt,
                UpdatedAt = card.UpdatedAt
            }).OrderByDescending(card => card.Status);
        }

        /// <summary>
        /// Changes the workflow status of a card after verifying board membership.
        /// </summary>
        /// <param name="cardId">
        /// The identifier of the card to update.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user requesting the status change.
        /// </param>
        /// <param name="newStatus">
        /// The new workflow status.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the card identifier is empty or the status is invalid.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the card does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the parent board.
        /// </exception>
        public async Task ChangeStatusAsync(Guid cardId, Guid userId, CardStatus newStatus)
        {
            if (cardId == Guid.Empty)
                throw new ArgumentException("CardId cannot be empty.");

            var card = await _cardRepository.GetByIdAsync(cardId) ?? throw new KeyNotFoundException("Card not found.");

            await _membershipAuthorizationService.EnsureCardBoardMemberAsync(cardId, userId);

            card.ChangeStatus(newStatus);

            await _cardRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Removes a card after verifying that the user belongs to its board.
        /// </summary>
        /// <param name="cardId">
        /// The identifier of the card to remove.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user requesting the removal.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
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
        public async Task RemoveAsync(Guid cardId, Guid userId)
        {
            if (cardId == Guid.Empty)
                throw new ArgumentException("CardId cannot be empty.");

            var card = await _cardRepository.GetByIdAsync(cardId);

            if (card is null)
                throw new KeyNotFoundException("Card not found.");

            await _membershipAuthorizationService.EnsureCardBoardMemberAsync(cardId, userId);

            await _cardRepository.RemoveAsync(card);

            await _cardRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Updates editable card fields after verifying board membership.
        /// </summary>
        /// <param name="cardId">
        /// The identifier of the card to update.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user requesting the update.
        /// </param>
        /// <param name="request">
        /// The card fields to update.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the card identifier is empty or a supplied domain value is invalid.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the card does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the parent board.
        /// </exception>
        public async Task UpdateAsync(Guid cardId, Guid userId, UpdateCardRequest request)
        {
            if (cardId == Guid.Empty)
                throw new ArgumentException("CardId cannot be empty.");

            var card = await _cardRepository.GetByIdAsync(cardId);

            if (card is null)
                throw new KeyNotFoundException("Card not found.");

            await _membershipAuthorizationService.EnsureCardBoardMemberAsync(cardId, userId);

            if (!string.IsNullOrWhiteSpace(request.Title))
                card.UpdateTitle(request.Title);

            if (request.Description is not null)
                card.UpdateDescription(request.Description);

            await _cardRepository.SaveChangesAsync();
        }
    }
}
