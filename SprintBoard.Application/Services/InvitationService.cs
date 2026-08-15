using SprintBoard.Application.Interfaces;
using SprintBoard.Domain.Entities;
using SprintBoard.Domain.Enums;

namespace SprintBoard.Application.Services
{
    /// <summary>
    /// Coordinates acceptance and rejection of board invitations.
    /// </summary>
    public class InvitationService
    {
        private readonly IBoardInvitationRepository _boardInvitationRepository;
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvitationService"/> class.
        /// </summary>
        /// <param name="boardInvitationRepository">
        /// Repository used to retrieve and update board invitations.
        /// </param>
        /// <param name="boardMemberRepository">
        /// Repository used to validate and persist board memberships created from accepted invitations.
        /// </param>
        /// <param name="userRepository">
        /// Repository used to validate the user responding to an invitation.
        /// </param>
        public InvitationService(
            IBoardInvitationRepository boardInvitationRepository,
            IBoardMemberRepository boardMemberRepository,
            IUserRepository userRepository)
        {
            _boardInvitationRepository = boardInvitationRepository;
            _boardMemberRepository = boardMemberRepository;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Accepts a valid pending invitation and adds the user to the board as a member.
        /// </summary>
        /// <param name="token">
        /// The token that identifies the invitation.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user accepting the invitation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the invitation token is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the invitation or user does not exist.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the invitation is not pending, has expired, or the user is already a board member.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the invitation email does not match the responding user.
        /// </exception>
        public async Task AcceptAsync(string token, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be empty.");

            var invitation = await _boardInvitationRepository.GetByTokenAsync(token);

            if (invitation is null)
                throw new KeyNotFoundException("Invitation not found.");

            if (invitation.Status != InvitationStatus.Pending)
                throw new InvalidOperationException("Invitation is no longer valid.");

            if (DateTime.UtcNow > invitation.ExpiresAt)
            {
                invitation.Expire();
                await _boardInvitationRepository.SaveChangesAsync();
                throw new InvalidOperationException("Invitation has expired.");
            }

            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null)
                throw new KeyNotFoundException("User not found.");

            if (user.Email != invitation.Email)
                throw new UnauthorizedAccessException("This invitation does not belong to your email.");

            var alreadyMember = await _boardMemberRepository.ExistsAsync(invitation.BoardId, userId);

            if (alreadyMember)
                throw new InvalidOperationException("User is already a member.");

            var boardMember = new BoardMember(invitation.BoardId, userId, BoardRole.Member);

            await _boardMemberRepository.AddAsync(boardMember);

            invitation.Accept();

            await _boardMemberRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Declines a valid pending invitation on behalf of the user associated with its email address.
        /// </summary>
        /// <param name="token">
        /// The token that identifies the invitation.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user declining the invitation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the invitation token is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the invitation or user does not exist.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the invitation email does not match the responding user.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the invitation is not pending or has expired.
        /// </exception>
        public async Task DeclineAsync(string token, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be empty.");

            var invitation = await _boardInvitationRepository.GetByTokenAsync(token);

            if (invitation is null)
                throw new KeyNotFoundException("Invitation not found.");

            if (invitation.Status != InvitationStatus.Pending)
                throw new InvalidOperationException("Invitation is no longer valid.");

            if (DateTime.UtcNow > invitation.ExpiresAt)
            {
                invitation.Expire();
                await _boardInvitationRepository.SaveChangesAsync();
                throw new InvalidOperationException("Invitation has expired.");
            }

            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null)
                throw new KeyNotFoundException("User not found.");

            if (user.Email != invitation.Email)
                throw new UnauthorizedAccessException("This invitation does not belong to your email.");

            invitation.Decline();

            await _boardInvitationRepository.SaveChangesAsync();
        }
    }
}
