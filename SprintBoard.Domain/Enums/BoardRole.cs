namespace SprintBoard.Domain.Enums;

/// <summary>
/// Defines the available roles for a board member.
/// </summary>
public enum BoardRole
{
    /// <summary>
    /// Grants full ownership permissions over the board.
    /// </summary>
    Owner = 1,

    /// <summary>
    /// Grants administrative permissions within the board.
    /// </summary>
    Admin = 2,

    /// <summary>
    /// Grants standard member permissions within the board.
    /// </summary>
    Member = 3
}
