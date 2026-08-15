namespace SprintBoard.Domain.Enums;

/// <summary>
/// Defines the workflow statuses available for a card.
/// </summary>
public enum CardStatus
{
    /// <summary>
    /// Indicates that work on the card has not started.
    /// </summary>
    ToDo = 1,

    /// <summary>
    /// Indicates that work on the card is in progress.
    /// </summary>
    Doing = 2,

    /// <summary>
    /// Indicates that work on the card is complete.
    /// </summary>
    Done = 3
}
