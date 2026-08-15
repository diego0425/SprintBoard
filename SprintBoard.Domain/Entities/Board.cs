namespace SprintBoard.Domain.Entities;

/// <summary>
/// Represents a board used to organize cards and members.
/// </summary>
public class Board
{
    /// <summary>
    /// Gets the board identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the board name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the identifier of the user who owns the board.
    /// </summary>
    public Guid OwnerId { get; private set; }

    /// <summary>
    /// Gets the user who owns the board.
    /// </summary>
    public User Owner { get; private set; } = null!;

    /// <summary>
    /// Gets the UTC date and time when the board was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the collection of users who belong to the board.
    /// </summary>
    public ICollection<BoardMember> Members { get; private set; } = new List<BoardMember>();

    /// <summary>
    /// Initializes a board instance for Entity Framework Core.
    /// </summary>
    protected Board()
    {
    }

    /// <summary>
    /// Initializes a new board.
    /// </summary>
    /// <param name="name">
    /// The board name.
    /// </param>
    /// <param name="ownerId">
    /// The identifier of the board owner.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the board name is empty or the owner identifier is invalid.
    /// </exception>
    public Board(string name, Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Board name cannot be empty.", nameof(name));

        if (ownerId == Guid.Empty)
            throw new ArgumentException("Owner identifier cannot be empty.", nameof(ownerId));

        Id = Guid.NewGuid();
        Name = name.Trim();
        OwnerId = ownerId;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the board name.
    /// </summary>
    /// <param name="name">
    /// The new board name.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the new name is empty.
    /// </exception>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Board name cannot be empty.", nameof(name));

        Name = name.Trim();
    }
}
