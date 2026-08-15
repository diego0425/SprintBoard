namespace SprintBoard.Application.DTOs.CardTask
{
    /// <summary>
    /// Represents the editable values of an existing checklist task.
    /// </summary>
    public sealed class UpdateCardTaskRequest
    {
        /// <summary>
        /// Gets or initializes the new checklist task title.
        /// </summary>
        public string Title { get; init; } = string.Empty;
    }
}
