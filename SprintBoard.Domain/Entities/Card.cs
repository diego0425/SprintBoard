using SprintBoard.Domain.Enums;

namespace SprintBoard.Domain.Entities;

/// <summary>
/// Represents a card within a board workflow.
/// </summary>
public class Card
{
    /// <summary>
    /// Gets the card identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the board identifier.
    /// </summary>
    public Guid BoardId { get; private set; }

    /// <summary>
    /// Gets the board containing the card.
    /// </summary>
    public Board Board { get; private set; } = null!;

    /// <summary>
    /// Gets the card title.
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Gets the optional card description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the current workflow status.
    /// </summary>
    public CardStatus Status { get; private set; }

    /// <summary>
    /// Gets the card position within its status column.
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the card was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the card was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the checklist tasks associated with the card.
    /// </summary>
    public ICollection<CardTask> Tasks { get; private set; } = new List<CardTask>();

    /// <summary>
    /// Initializes a card instance for Entity Framework Core.
    /// </summary>
    protected Card()
    {
    }

    /// <summary>
    /// Initializes a new card.
    /// </summary>
    /// <param name="boardId">
    /// The board identifier.
    /// </param>
    /// <param name="title">
    /// The card title.
    /// </param>
    /// <param name="description">
    /// The optional card description.
    /// </param>
    /// <param name="position">
    /// The initial position within the status column.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the board identifier, title, or position is invalid.
    /// </exception>
    public Card(Guid boardId, string title, string? description = null, int position = 0)
    {
        if (boardId == Guid.Empty)
            throw new ArgumentException("Board identifier cannot be empty.", nameof(boardId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        if (position < 0)
            throw new ArgumentException("Position cannot be negative.", nameof(position));

        var createdAtUtc = DateTime.UtcNow;

        Id = Guid.NewGuid();
        BoardId = boardId;
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Status = CardStatus.ToDo;
        Position = position;
        CreatedAt = createdAtUtc;
        UpdatedAt = createdAtUtc;
    }

    /// <summary>
    /// Updates the card title.
    /// </summary>
    /// <param name="title">
    /// The new card title.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the title is empty.
    /// </exception>
    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        Title = title.Trim();
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates or removes the card description.
    /// </summary>
    /// <param name="description">
    /// The new description, or an empty value to remove it.
    /// </param>
    public void UpdateDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdateTimestamp();
    }

    /// <summary>
    /// Changes the card workflow status.
    /// </summary>
    /// <param name="status">
    /// The new card status.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the status is invalid.
    /// </exception>
    public void ChangeStatus(CardStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentException("Card status is invalid.", nameof(status));

        Status = status;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the card position within its status column.
    /// </summary>
    /// <param name="position">
    /// The new zero-based position.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the position is negative.
    /// </exception>
    public void UpdatePosition(int position)
    {
        if (position < 0)
            throw new ArgumentException("Position cannot be negative.", nameof(position));

        Position = position;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the last-modified timestamp.
    /// </summary>
    private void UpdateTimestamp() => UpdatedAt = DateTime.UtcNow;
}
