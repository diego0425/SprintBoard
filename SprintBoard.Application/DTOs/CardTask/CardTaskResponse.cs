namespace SprintBoard.Application.DTOs.CardTask
{
    /// <summary>
    /// Represents checklist task data returned by the application layer.
    /// </summary>
    public sealed class CardTaskResponse
    {
        /// <summary>
        /// Gets or initializes the checklist task identifier.
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// Gets or initializes the identifier of the parent card.
        /// </summary>
        public Guid CardId { get; init; }
        /// <summary>
        /// Gets or initializes the checklist task title.
        /// </summary>
        public string Title { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes a value indicating whether the checklist task is completed.
        /// </summary>
        public bool IsCompleted { get; init; }
        /// <summary>
        /// Gets or initializes the checklist task position within the card.
        /// </summary>
        public int Position { get; init; }
        /// <summary>
        /// Gets or initializes the UTC date and time when the checklist task was created.
        /// </summary>
        public DateTime CreatedAt { get; init; }
        /// <summary>
        /// Gets or initializes the UTC date and time when the checklist task was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; init; }
    }
}
