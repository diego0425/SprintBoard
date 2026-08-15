namespace SprintBoard.Application.Interfaces
{
    /// <summary>
    /// Defines URL-building operations for board invitation actions.
    /// </summary>
    public interface IInvitationLinkBuilder
    {
        /// <summary>
        /// Builds the frontend URL used to accept an invitation.
        /// </summary>
        /// <param name="token">
        /// The invitation token embedded in the URL.
        /// </param>
        /// <returns>
        /// The complete invitation acceptance URL.
        /// </returns>
        string BuildAcceptInvitationLink(string token);
        /// <summary>
        /// Builds the frontend URL used to decline an invitation.
        /// </summary>
        /// <param name="token">
        /// The invitation token embedded in the URL.
        /// </param>
        /// <returns>
        /// The complete invitation decline URL.
        /// </returns>
        string BuildDeclineInvitationLink(string token);
    }
}
