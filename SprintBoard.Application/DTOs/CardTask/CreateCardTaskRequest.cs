namespace SprintBoard.Application.DTOs.CardTask
{
    /// <summary>
    /// Represents the information required to create a checklist task for a card.
    /// </summary>
    public sealed class CreateCardTaskRequest
    {
        /// <summary>
        /// Gets or initializes the checklist task title.
        /// </summary>
        public string Title { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the optional initial position of the checklist task. A missing value defaults to zero.
        /// </summary>
        public int? Position { get; init; }
    }
}
