using SprintBoard.Domain.Enums;

namespace SprintBoard.Domain.Entities;

/// <summary>
/// Represents a user's membership and role within a board.
/// </summary>
public class BoardMember
{
    /// <summary>
    /// Gets the board membership identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the board identifier.
    /// </summary>
    public Guid BoardId { get; private set; }

    /// <summary>
    /// Gets the member user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the member's role in the board.
    /// </summary>
    public BoardRole Role { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the user joined the board.
    /// </summary>
    public DateTime JoinedAt { get; private set; }

    /// <summary>
    /// Gets the board associated with the membership.
    /// </summary>
    public Board Board { get; private set; } = null!;

    /// <summary>
    /// Gets the user associated with the membership.
    /// </summary>
    public User User { get; private set; } = null!;

    /// <summary>
    /// Initializes a board membership instance for Entity Framework Core.
    /// </summary>
    protected BoardMember()
    {
    }

    /// <summary>
    /// Initializes a new board membership.
    /// </summary>
    /// <param name="boardId">
    /// The board identifier.
    /// </param>
    /// <param name="userId">
    /// The member user identifier.
    /// </param>
    /// <param name="role">
    /// The member's board role.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when an identifier or role is invalid.
    /// </exception>
    public BoardMember(Guid boardId, Guid userId, BoardRole role)
    {
        if (boardId == Guid.Empty)
            throw new ArgumentException("Board identifier cannot be empty.", nameof(boardId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));

        if (!Enum.IsDefined(role))
            throw new ArgumentException("Board role is invalid.", nameof(role));

        Id = Guid.NewGuid();
        BoardId = boardId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Changes the member's role.
    /// </summary>
    /// <param name="newRole">
    /// The new board role.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the new role is invalid.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to change the owner's role.
    /// </exception>
    public void ChangeRole(BoardRole newRole)
    {
        if (!Enum.IsDefined(newRole))
            throw new ArgumentException("Board role is invalid.", nameof(newRole));

        if (Role == BoardRole.Owner)
            throw new InvalidOperationException("Owner role cannot be changed.");

        Role = newRole;
    }
}
