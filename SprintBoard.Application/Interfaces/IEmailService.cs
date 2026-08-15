namespace SprintBoard.Application.Interfaces
{
    /// <summary>
    /// Defines email operations required by application workflows.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email that allows a recipient to accept or decline a board invitation.
        /// </summary>
        /// <param name="toEmail">
        /// The recipient email address.
        /// </param>
        /// <param name="boardName">
        /// The name of the board the recipient was invited to join.
        /// </param>
        /// <param name="acceptInvitationLink">
        /// The URL used to accept the invitation.
        /// </param>
        /// <param name="declineInvitationLink">
        /// The URL used to decline the invitation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task SendBoardInvitationAsync(
            string toEmail, 
            string boardName, 
            string acceptInvitationLink, 
            string declineInvitationLink);
    }
}
