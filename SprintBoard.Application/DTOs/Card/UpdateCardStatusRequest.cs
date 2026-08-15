using SprintBoard.Domain.Enums;

namespace SprintBoard.Application.DTOs.Card
{
    /// <summary>
    /// Represents a request to change a card's workflow status.
    /// </summary>
    public sealed class UpdateCardStatusRequest
    {
        /// <summary>
        /// Gets or initializes the new workflow status for the card.
        /// </summary>
        public CardStatus Status { get; init; }
    }
}
