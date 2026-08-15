namespace SprintBoard.Domain.Enums;

/// <summary>
/// Defines the lifecycle statuses available for a board invitation.
/// </summary>
public enum InvitationStatus
{
    /// <summary>
    /// Indicates that the invitation is awaiting a response.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Indicates that the invitation was accepted.
    /// </summary>
    Accepted = 2,

    /// <summary>
    /// Indicates that the invitation was declined.
    /// </summary>
    Declined = 3,

    /// <summary>
    /// Indicates that the invitation expired before being answered.
    /// </summary>
    Expired = 4
}
