namespace SprintBoard.Application.DTOs.Card
{
    /// <summary>
    /// Represents the information required to create a card.
    /// </summary>
    public sealed class CreateCardRequest
    {
        /// <summary>
        /// Gets or initializes the card title.
        /// </summary>
        public string Title { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the optional card description.
        /// </summary>
        public string? Description { get; init; }
        /// <summary>
        /// Gets or initializes the optional initial position of the card. A missing value defaults to zero.
        /// </summary>
        public int? Position { get; init; }
    }
}
