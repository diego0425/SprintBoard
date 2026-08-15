namespace SprintBoard.Application.DTOs.Board
{
    /// <summary>
    /// Represents board data returned by the application layer.
    /// </summary>
    public sealed class BoardResponse
    {
        /// <summary>
        /// Gets or initializes the board identifier.
        /// </summary>
        public Guid Id { get; init; }
        /// <summary>
        /// Gets or initializes the board name.
        /// </summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>
        /// Gets or initializes the identifier of the user who owns the board.
        /// </summary>
        public Guid OwnerId { get; init; }
        /// <summary>
        /// Gets or initializes the UTC date and time when the board was created.
        /// </summary>
        public DateTime CreatedAt { get; init; }
    }
}
