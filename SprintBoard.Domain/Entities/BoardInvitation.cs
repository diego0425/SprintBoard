using SprintBoard.Domain.Enums;

namespace SprintBoard.Domain.Entities;

/// <summary>
/// Represents an invitation sent to a user to join a board.
/// </summary>
public class BoardInvitation
{
    /// <summary>
    /// Gets the invitation identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the invited board.
    /// </summary>
    public Guid BoardId { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who sent the invitation.
    /// </summary>
    public Guid InvitedByUserId { get; private set; }

    /// <summary>
    /// Gets the invited email address.
    /// </summary>
    public string Email { get; private set; } = null!;

    /// <summary>
    /// Gets the invitation token.
    /// </summary>
    public string Token { get; private set; } = null!;

    /// <summary>
    /// Gets the current invitation status.
    /// </summary>
    public InvitationStatus Status { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the invitation expires.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the invitation was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the board associated with the invitation.
    /// </summary>
    public Board Board { get; private set; } = null!;

    /// <summary>
    /// Gets the user who sent the invitation.
    /// </summary>
    public User InvitedByUser { get; private set; } = null!;

    /// <summary>
    /// Initializes an invitation instance for Entity Framework Core.
    /// </summary>
    protected BoardInvitation()
    {
    }

    /// <summary>
    /// Initializes a new board invitation.
    /// </summary>
    /// <param name="boardId">
    /// The invited board identifier.
    /// </param>
    /// <param name="invitedByUserId">
    /// The identifier of the user sending the invitation.
    /// </param>
    /// <param name="email">
    /// The invited email address.
    /// </param>
    /// <param name="token">
    /// The unique invitation token.
    /// </param>
    /// <param name="expiresAt">
    /// The UTC expiration date and time.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when any required value is invalid.
    /// </exception>
    public BoardInvitation(
        Guid boardId,
        Guid invitedByUserId,
        string email,
        string token,
        DateTime expiresAt)
    {
        if (boardId == Guid.Empty)
            throw new ArgumentException("Board identifier cannot be empty.", nameof(boardId));

        if (invitedByUserId == Guid.Empty)
            throw new ArgumentException("Inviting user identifier cannot be empty.", nameof(invitedByUserId));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty.", nameof(token));

        Id = Guid.NewGuid();
        BoardId = boardId;
        InvitedByUserId = invitedByUserId;
        Email = email.Trim().ToLowerInvariant();
        Token = token;
        ExpiresAt = expiresAt;
        Status = InvitationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Accepts the invitation when it is pending and has not expired.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the invitation is not pending or has expired.
    /// </exception>
    public void Accept()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException("Invitation is not pending.");

        if (DateTime.UtcNow > ExpiresAt)
            throw new InvalidOperationException("Invitation has expired.");

        Status = InvitationStatus.Accepted;
    }

    /// <summary>
    /// Declines the invitation when it is pending and has not expired.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the invitation is not pending or has expired.
    /// </exception>
    public void Decline()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException("Invitation is not pending.");

        if (DateTime.UtcNow > ExpiresAt)
            throw new InvalidOperationException("Invitation has expired.");

        Status = InvitationStatus.Declined;
    }

    /// <summary>
    /// Marks a pending invitation as expired.
    /// </summary>
    public void Expire()
    {
        if (Status == InvitationStatus.Pending)
            Status = InvitationStatus.Expired;
    }
}
