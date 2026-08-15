namespace SprintBoard.Application.DTOs.Card
{
    /// <summary>
    /// Represents the editable values of an existing card.
    /// </summary>
    public sealed class UpdateCardRequest
    {
        /// <summary>
        /// Gets or initializes the new card title, or <see langword="null"/> to leave it unchanged.
        /// </summary>
        public string? Title { get; init; }
        /// <summary>
        /// Gets or initializes the new card description. A non-null value replaces the current description.
        /// </summary>
        public string? Description { get; init; }
    }
}
