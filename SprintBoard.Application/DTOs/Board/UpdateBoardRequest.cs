namespace SprintBoard.Application.DTOs.Board
{
    /// <summary>
    /// Represents the editable values of an existing board.
    /// </summary>
    public sealed class UpdateBoardRequest
    {
        /// <summary>
        /// Gets or initializes the new board name, or <see langword="null"/> to leave it unchanged.
        /// </summary>
        public string? Name { get; init; }
    }
}
