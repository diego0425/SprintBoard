namespace SprintBoard.Domain.Entities;

/// <summary>
/// Represents a checklist task associated with a card.
/// </summary>
public class CardTask
{
    /// <summary>
    /// Gets the checklist task identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the parent card identifier.
    /// </summary>
    public Guid CardId { get; private set; }

    /// <summary>
    /// Gets the checklist task title.
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Gets a value indicating whether the checklist task is completed.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Gets the checklist task position.
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the checklist task was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the checklist task was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the parent card.
    /// </summary>
    public Card Card { get; private set; } = null!;

    /// <summary>
    /// Initializes a checklist task instance for Entity Framework Core.
    /// </summary>
    protected CardTask()
    {
    }

    /// <summary>
    /// Initializes a new checklist task.
    /// </summary>
    /// <param name="cardId">
    /// The parent card identifier.
    /// </param>
    /// <param name="title">
    /// The checklist task title.
    /// </param>
    /// <param name="position">
    /// The initial checklist position.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the card identifier, title, or position is invalid.
    /// </exception>
    public CardTask(Guid cardId, string title, int position = 0)
    {
        if (cardId == Guid.Empty)
            throw new ArgumentException("Card identifier cannot be empty.", nameof(cardId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        if (position < 0)
            throw new ArgumentException("Position cannot be negative.", nameof(position));

        var createdAtUtc = DateTime.UtcNow;

        Id = Guid.NewGuid();
        CardId = cardId;
        Title = title.Trim();
        IsCompleted = false;
        Position = position;
        CreatedAt = createdAtUtc;
        UpdatedAt = createdAtUtc;
    }

    /// <summary>
    /// Updates the checklist task title.
    /// </summary>
    /// <param name="title">
    /// The new checklist task title.
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
    /// Marks the checklist task as completed.
    /// </summary>
    public void MarkAsCompleted()
    {
        IsCompleted = true;
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks the checklist task as pending.
    /// </summary>
    public void MarkAsPending()
    {
        IsCompleted = false;
        UpdateTimestamp();
    }

    /// <summary>
    /// Moves the checklist task to a new position.
    /// </summary>
    /// <param name="position">
    /// The new zero-based position.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the position is negative.
    /// </exception>
    public void MoveToPosition(int position)
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
