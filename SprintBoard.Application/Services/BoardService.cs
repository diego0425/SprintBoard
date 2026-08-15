using SprintBoard.Application.DTOs.Board;
using SprintBoard.Application.DTOs.Card;
using SprintBoard.Application.Interfaces;
using SprintBoard.Domain.Entities;
using SprintBoard.Domain.Enums;
using System.Security.Cryptography;

namespace SprintBoard.Application.Services
{
    /// <summary>
    /// Coordinates board creation, retrieval, membership administration, invitations, updates, and removal.
    /// </summary>
    public sealed class BoardService
    {
        private readonly IBoardRepository _boardRepository;
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBoardInvitationRepository _boardInvitationRepository;
        private readonly IEmailService _emailService;
        private readonly IMembershipAuthorizationService _membershipAuthorizationService;
        private readonly IInvitationLinkBuilder _invitationLinkBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoardService"/> class.
        /// </summary>
        /// <param name="boardRepository">
        /// Repository used to query, add, update, and remove boards.
        /// </param>
        /// <param name="userRepository">
        /// Repository used to validate users involved in board operations.
        /// </param>
        /// <param name="boardMemberRepository">
        /// Repository used to query and persist board memberships and roles.
        /// </param>
        /// <param name="boardInvitationRepository">
        /// Repository used to validate and persist board invitations.
        /// </param>
        /// <param name="emailService">
        /// Email service used to deliver board invitations.
        /// </param>
        /// <param name="membershipAuthorizationService">
        /// Authorization service used to enforce board ownership and membership rules.
        /// </param>
        /// <param name="invitationLinkBuilder">
        /// Link builder used to generate accept and decline URLs for invitation emails.
        /// </param>
        public BoardService(
            IBoardRepository boardRepository, 
            IUserRepository userRepository, 
            IBoardMemberRepository boardMemberRepository, 
            IBoardInvitationRepository boardInvitationRepository,
            IEmailService emailService,
            IMembershipAuthorizationService membershipAuthorizationService,
            IInvitationLinkBuilder invitationLinkBuilder)
        {
            _boardRepository = boardRepository;
            _userRepository = userRepository;
            _boardMemberRepository = boardMemberRepository;
            _boardInvitationRepository = boardInvitationRepository;
            _emailService = emailService;
            _membershipAuthorizationService = membershipAuthorizationService;
            _invitationLinkBuilder = invitationLinkBuilder;
        }

        /// <summary>
        /// Creates a board and registers its creator as the board owner.
        /// </summary>
        /// <param name="name">
        /// The name of the board to create.
        /// </param>
        /// <param name="ownerId">
        /// The identifier of the user who will own the board.
        /// </param>
        /// <returns>
        /// A response containing the newly created board data.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the board name is empty or the owner identifier is invalid.
        /// </exception>
        public async Task<BoardResponse> CreateAsync(string name, Guid ownerId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Board name cannot be empty.");

            if (ownerId == Guid.Empty)
                throw new ArgumentException("OwnerId cannot be empty.");

            var board = new Board(name, ownerId);

            await _boardRepository.AddAsync(board);
            await _boardRepository.SaveChangesAsync();

            var ownerMember = new BoardMember(board.Id, ownerId, BoardRole.Owner);
            await _boardMemberRepository.AddAsync(ownerMember);
            await _boardMemberRepository.SaveChangesAsync();

            return new BoardResponse
            {
                Id = board.Id,
                Name = board.Name,
                OwnerId = board.OwnerId,
                CreatedAt = board.CreatedAt
            };
        }

        /// <summary>
        /// Retrieves a board by identifier without performing a membership authorization check.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board to retrieve.
        /// </param>
        /// <returns>
        /// The matching board data.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the board identifier is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the board does not exist.
        /// </exception>
        public async Task<BoardResponse> GetByIdAsync(Guid boardId)
        {
            if (boardId == Guid.Empty)
                throw new ArgumentException("BoardId cannot be empty.");

            var board = await _boardRepository.GetByIdAsync(boardId);

            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            return new BoardResponse
            {
                Id = board.Id,
                Name = board.Name,
                OwnerId = board.OwnerId,
                CreatedAt = board.CreatedAt
            };
        }

        /// <summary>
        /// Determines whether a user has a membership on an existing board.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board to check.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user whose membership will be checked.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the user belongs to the board; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when either identifier is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the board does not exist.
        /// </exception>
        public async Task<bool> ExistsAsync(Guid boardId, Guid userId)
        {
            if (boardId == Guid.Empty)
                throw new ArgumentException("BoardId cannot be empty.");

            if (userId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty.");

            var board = await _boardRepository.GetByIdAsync(boardId);

            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            return await _boardMemberRepository.ExistsAsync(boardId, userId);

        }

        /// <summary>
        /// Creates and sends a board invitation on behalf of a board owner or administrator.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board to which the recipient is being invited.
        /// </param>
        /// <param name="requesterUserId">
        /// The identifier of the owner or administrator creating the invitation.
        /// </param>
        /// <param name="email">
        /// The email address of the user being invited.
        /// </param>
        /// <returns>
        /// The newly created invitation data.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an identifier is empty or the email address is missing.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the board does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the requester is neither the board owner nor an administrator.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the invited user is already a board member or a pending invitation already exists.
        /// </exception>
        public async Task<BoardInvitationResponse> CreateInvitationAsync(
            Guid boardId,
            Guid requesterUserId,
            string email)
        {
            if (boardId == Guid.Empty)
                throw new ArgumentException("BoardId cannot be empty.");

            if (requesterUserId == Guid.Empty)
                throw new ArgumentException("Requester user id cannot be empty.");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty.");

            var board = await _boardRepository.GetByIdAsync(boardId);

            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            await _membershipAuthorizationService.EnsureBoardOwnerOrAdminAsync(
                boardId,
                requesterUserId);

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var invitedUser = await _userRepository.GetByEmailAsync(normalizedEmail);

            if (invitedUser is not null)
            {
                var alreadyMember = await _boardMemberRepository.ExistsAsync(
                    boardId,
                    invitedUser.Id);

                if (alreadyMember)
                    throw new InvalidOperationException("User is already a member of this board.");
            }

            var alreadyInvited = await _boardInvitationRepository.ExistsPendingAsync(
                boardId,
                normalizedEmail);

            if (alreadyInvited)
                throw new InvalidOperationException("There is already a pending invitation for this email.");

            var token = GenerateToken();
            var expiresAt = DateTime.UtcNow.AddDays(7);

            var invitation = new BoardInvitation(
                boardId,
                requesterUserId,
                normalizedEmail,
                token,
                expiresAt);

            await _boardInvitationRepository.AddAsync(invitation);
            await _boardInvitationRepository.SaveChangesAsync();

            var acceptInvitationLink = _invitationLinkBuilder.BuildAcceptInvitationLink(invitation.Token);
            var declineInvitationLink = _invitationLinkBuilder.BuildDeclineInvitationLink(invitation.Token);

            await _emailService.SendBoardInvitationAsync(
                invitation.Email,
                board.Name,
                acceptInvitationLink,
                declineInvitationLink);

            return new BoardInvitationResponse
            {
                Id = invitation.Id,
                BoardId = invitation.BoardId,
                Email = invitation.Email,
                Token = invitation.Token,
                ExpiresAt = invitation.ExpiresAt,
                CreatedAt = invitation.CreatedAt
            };
        }

        /// <summary>
        /// Generates a cryptographically random token for a board invitation.
        /// </summary>
        /// <returns>
        /// A hexadecimal invitation token.
        /// </returns>
        private static string GenerateToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }

        /// <summary>
        /// Changes the role of a board member when requested by the board owner.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board that contains the member.
        /// </param>
        /// <param name="requesterUserId">
        /// The identifier of the user requesting the role change.
        /// </param>
        /// <param name="memberUserId">
        /// The identifier of the member whose role will be changed.
        /// </param>
        /// <param name="newRoleValue">
        /// The numeric value of the new <see cref="BoardRole"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the requester is not the board owner.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the target member does not exist.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when an attempt is made to change the owner's role or assign the Owner role to another member.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown by the domain entity when the new role value is not defined.
        /// </exception>
        public async Task ChangeMemberRoleAsync(Guid boardId, Guid requesterUserId, Guid memberUserId, int newRoleValue)
        {
            var newRole = (BoardRole)newRoleValue;

            await _membershipAuthorizationService.EnsureBoardOwnerAsync(boardId, requesterUserId);

            var member = await _boardMemberRepository.GetMemberAsync(boardId, memberUserId);

            if (member is null)
                throw new KeyNotFoundException("Member not found.");

            if (member.Role == BoardRole.Owner)
                throw new InvalidOperationException("Cannot change the owner's role.");

            if (newRole == BoardRole.Owner)
                throw new InvalidOperationException("The Owner role cannot be assigned to another member.");

            member.ChangeRole(newRole);

            await _boardMemberRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves all boards in which a user has a membership.
        /// </summary>
        /// <param name="userId">
        /// The identifier of the user whose boards will be retrieved.
        /// </param>
        /// <returns>
        /// The boards available through the user's memberships.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the user identifier is empty.
        /// </exception>
        public async Task<IEnumerable<BoardResponse>> GetByUserAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty.");

            var boards = await _boardRepository.GetByUserMembershipAsync(userId);

            return boards.Select(board => new BoardResponse
            {
                Id = board.Id,
                Name = board.Name,
                OwnerId = board.OwnerId,
                CreatedAt = board.CreatedAt
            });
        }

        /// <summary>
        /// Retrieves a board after verifying that the requesting user is a board member.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board to retrieve.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user requesting access.
        /// </param>
        /// <returns>
        /// The matching board data.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the board identifier is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the board does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the user is not a member of the board.
        /// </exception>
        public async Task<BoardResponse> GetByIdAsync(Guid boardId, Guid userId)
        {
            if (boardId == Guid.Empty)
                throw new ArgumentException("BoardId cannot be empty.");

            var board = await _boardRepository.GetByIdAsync(boardId);

            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            await _membershipAuthorizationService.EnsureBoardMemberAsync(boardId, userId);

            return new BoardResponse
            {
                Id = board.Id,
                Name = board.Name,
                OwnerId = board.OwnerId,
                CreatedAt = board.CreatedAt
            };
        }

        /// <summary>
        /// Removes a board after verifying that the requesting user is its owner.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board to remove.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user requesting the removal.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the board identifier is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the board does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the requesting user is not the board owner.
        /// </exception>
        public async Task RemoveAsync(Guid boardId, Guid userId)
        {
            if (boardId == Guid.Empty)
                throw new ArgumentException("BoardId cannot be empty.");

            var board = await _boardRepository.GetByIdAsync(boardId);

            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            await _membershipAuthorizationService.EnsureBoardOwnerAsync(boardId, userId);

            await _boardRepository.RemoveAsync(board);

            await _boardRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Updates editable board data after verifying that the requesting user is the board owner.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board to update.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user requesting the update.
        /// </param>
        /// <param name="request">
        /// The board values to update.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the board identifier is empty or the supplied name is invalid at the domain level.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the board does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the requesting user is not the board owner.
        /// </exception>
        public async Task UpdateAsync(Guid boardId, Guid userId, UpdateBoardRequest request)
        {
            if (boardId == Guid.Empty)
                throw new ArgumentException("BoardId cannot be empty.");

            var board = await _boardRepository.GetByIdAsync(boardId);

            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            await _membershipAuthorizationService.EnsureBoardOwnerAsync(boardId, userId);

            if (!string.IsNullOrWhiteSpace(request.Name))
                board.UpdateName(request.Name);

            await _boardRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Removes a member from a board according to the requester's administrative role.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board from which the member will be removed.
        /// </param>
        /// <param name="requesterUserId">
        /// The identifier of the owner or administrator requesting the removal.
        /// </param>
        /// <param name="memberUserId">
        /// The identifier of the member to remove.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when any identifier is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the board, requester membership, or target membership does not exist.
        /// </exception>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the requester lacks permission to remove the target member.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when an attempt is made to remove the board owner.
        /// </exception>
        public async Task RemoveMemberAsync(
            Guid boardId,
            Guid requesterUserId,
            Guid memberUserId)
        {
            if (boardId == Guid.Empty)
                throw new ArgumentException("BoardId cannot be empty.");

            if (requesterUserId == Guid.Empty)
                throw new ArgumentException("Requester user id cannot be empty.");

            if (memberUserId == Guid.Empty)
                throw new ArgumentException("Member user id cannot be empty.");

            var board = await _boardRepository.GetByIdAsync(boardId);

            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            await _membershipAuthorizationService.EnsureBoardOwnerOrAdminAsync(
                boardId,
                requesterUserId);

            var requesterMembership = await _boardMemberRepository.GetMemberAsync(
                boardId,
                requesterUserId);

            if (requesterMembership is null)
                throw new KeyNotFoundException("Requester membership not found.");

            var memberToRemove = await _boardMemberRepository.GetMemberAsync(
                boardId,
                memberUserId);

            if (memberToRemove is null)
                throw new KeyNotFoundException("Member not found.");

            if (memberToRemove.Role == BoardRole.Owner)
                throw new InvalidOperationException("The board owner cannot be removed.");

            if (requesterMembership.Role == BoardRole.Admin &&
                memberToRemove.Role != BoardRole.Member)
            {
                throw new SprintBoard.Application.Exceptions.ForbiddenAccessException(
                    "Administrators can only remove members with the Member role.");
            }

            await _boardMemberRepository.RemoveAsync(memberToRemove);
            await _boardMemberRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Removes the authenticated user's own membership from a board.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board to leave.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user leaving the board.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an identifier is empty.
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the board or membership does not exist.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the board owner attempts to leave the board.
        /// </exception>
        public async Task LeaveBoardAsync(Guid boardId, Guid userId)
        {
            if (boardId == Guid.Empty)
                throw new ArgumentException("BoardId cannot be empty.");

            if (userId == Guid.Empty)
                throw new ArgumentException("UserId cannot be empty.");

            var board = await _boardRepository.GetByIdAsync(boardId);

            if (board is null)
                throw new KeyNotFoundException("Board not found.");

            var membership = await _boardMemberRepository.GetMemberAsync(boardId, userId);

            if (membership is null)
                throw new KeyNotFoundException("Board membership not found.");

            if (membership.Role == BoardRole.Owner)
                throw new InvalidOperationException(
                    "The board owner cannot leave the board. Delete the board instead.");

            await _boardMemberRepository.RemoveAsync(membership);
            await _boardMemberRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves the members of a board after verifying requester membership.
        /// </summary>
        /// <param name="boardId">
        /// The identifier of the board whose members will be retrieved.
        /// </param>
        /// <param name="userId">
        /// The identifier of the user requesting the member list.
        /// </param>
        /// <returns>
        /// The users and roles associated with the board.
        /// </returns>
        /// <exception cref="SprintBoard.Application.Exceptions.ForbiddenAccessException">
        /// Thrown when the requesting user is not a member of the board.
        /// </exception>
        public async Task<IEnumerable<BoardMemberResponse>> GetBoardMembersAsync(Guid boardId, Guid userId)
        {
            await _membershipAuthorizationService.EnsureBoardMemberAsync(boardId, userId);

            var members = await _boardMemberRepository.GetMembersAsync(boardId);

            return members.Select(m => new BoardMemberResponse
            {
                UserId = m.UserId,
                Username = m.User.Username,
                Role = m.Role,
                ProfileImageUrl = m.User.ProfileImageUrl
            });
        }
    }
}
