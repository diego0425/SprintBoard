namespace SprintBoard.Application.DTOs.Board
{
    /// <summary>
    /// Represents the information required to create a board.
    /// </summary>
    public sealed class CreateBoardRequest
    {
        /// <summary>
        /// Gets or initializes the name of the board to create.
        /// </summary>
        public string Name { get; init; } = string.Empty;
    }
}
