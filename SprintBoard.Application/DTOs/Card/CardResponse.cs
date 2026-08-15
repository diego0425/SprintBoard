using SprintBoard.Domain.Enums;

namespace SprintBoard.Application.DTOs.Card
{
    /// <summary>
    /// Represents card data returned by the application layer.
    /// </summary>
    public sealed class CardResponse
    {
        /// <summary>
        /// Gets or initializes the card identifier.
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// Gets or initializes the identifier of the board that contains the card.
        /// </summary>
        public Guid BoardId { get; init; }
        /// <summary>
        /// Gets or initializes the card title.
        /// </summary>
        public string Title { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the optional card description.
        /// </summary>
        public string? Description { get; init; }
        /// <summary>
        /// Gets or initializes the current workflow status of the card.
        /// </summary>
        public CardStatus Status { get; init; }
        /// <summary>
        /// Gets or initializes the card position within its status column.
        /// </summary>
        public int Position { get; init; }
        /// <summary>
        /// Gets or initializes the UTC date and time when the card was created.
        /// </summary>
        public DateTime CreatedAt { get; init; }
        /// <summary>
        /// Gets or initializes the UTC date and time when the card was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; init; }
    }
}
