using Microsoft.AspNetCore.Mvc;
using Moq;
using SprintBoard.api.Controllers;
using SprintBoard.api.Services;
using SprintBoard.Application.DTOs.Invitation;
using SprintBoard.Application.Interfaces;
using SprintBoard.Application.Services;
using SprintBoard.Domain.Entities;
using SprintBoard.Domain.Enums;
using Xunit;

namespace SprintBoard.Test.Controllers
{
    /// <summary>
    /// Contains tests for the <see cref="InvitationsController"/>.
    /// </summary>
    public class InvitationsControllerTests
    {
        private readonly Mock<IBoardInvitationRepository>
            _boardInvitationRepositoryMock;

        private readonly Mock<IBoardMemberRepository>
            _boardMemberRepositoryMock;

        private readonly Mock<IUserRepository>
            _userRepositoryMock;

        private readonly Mock<ICurrentUserService>
            _currentUserServiceMock;

        private readonly InvitationService _invitationService;
        private readonly InvitationsController _controller;

        /// <summary>
        /// Initializes the mocked dependencies, invitation service,
        /// and controller instance used by the invitation controller tests.
        /// </summary>
        public InvitationsControllerTests()
        {
            _boardInvitationRepositoryMock =
                new Mock<IBoardInvitationRepository>();

            _boardMemberRepositoryMock =
                new Mock<IBoardMemberRepository>();

            _userRepositoryMock =
                new Mock<IUserRepository>();

            _currentUserServiceMock =
                new Mock<ICurrentUserService>();

            _invitationService =
                new InvitationService(
                    _boardInvitationRepositoryMock.Object,
                    _boardMemberRepositoryMock.Object,
                    _userRepositoryMock.Object);

            _controller =
                new InvitationsController(
                    _invitationService,
                    _currentUserServiceMock.Object);

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.AddAsync(
                        It.IsAny<BoardMember>()))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.SaveChangesAsync())
                .Returns(Task.CompletedTask);
        }

        // ============================================================
        // ACCEPT
        // ============================================================

        /// <summary>
        /// Verifies that Accept returns HTTP 204 when a valid
        /// pending invitation is accepted by the invited user.
        /// </summary>
        [Fact]
        public async Task Accept_ShouldReturnNoContent_WhenInvitationIsValid()
        {
            // Arrange
            var user =
                CreateUser(
                    email: "member@example.com");

            var invitation =
                CreateInvitation(
                    email: user.Email);

            var request =
                new RespondToInvitationRequest
                {
                    Token = invitation.Token
                };

            SetupCurrentUser(user.Id);

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(
                        invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(
                        user.Id))
                .ReturnsAsync(user);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.ExistsAsync(
                        invitation.BoardId,
                        user.Id))
                .ReturnsAsync(false);

            // Act
            var result =
                await _controller.Accept(request);

            // Assert
            Assert.IsType<NoContentResult>(result);

            Assert.Equal(
                InvitationStatus.Accepted,
                invitation.Status);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Accept creates a board membership with the
        /// Member role for the user accepting a valid invitation.
        /// </summary>
        [Fact]
        public async Task Accept_ShouldCreateMemberWithMemberRole()
        {
            // Arrange
            var user =
                CreateUser(
                    email: "member@example.com");

            var invitation =
                CreateInvitation(
                    email: user.Email);

            var request =
                new RespondToInvitationRequest
                {
                    Token = invitation.Token
                };

            SetupCurrentUser(user.Id);

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(
                        invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(
                        user.Id))
                .ReturnsAsync(user);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.ExistsAsync(
                        invitation.BoardId,
                        user.Id))
                .ReturnsAsync(false);

            // Act
            await _controller.Accept(request);

            // Assert
            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.Is<BoardMember>(
                            member =>
                                member.BoardId ==
                                    invitation.BoardId &&
                                member.UserId ==
                                    user.Id &&
                                member.Role ==
                                    BoardRole.Member)),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Accept uses the identifier of the currently
        /// authenticated user when processing the invitation.
        /// </summary>
        [Fact]
        public async Task Accept_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var user =
                CreateUser(
                    email: "member@example.com");

            var invitation =
                CreateInvitation(
                    email: user.Email);

            var request =
                new RespondToInvitationRequest
                {
                    Token = invitation.Token
                };

            SetupCurrentUser(user.Id);

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(
                        invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(
                        user.Id))
                .ReturnsAsync(user);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.ExistsAsync(
                        invitation.BoardId,
                        user.Id))
                .ReturnsAsync(false);

            // Act
            await _controller.Accept(request);

            // Assert
            _currentUserServiceMock.Verify(
                service =>
                    service.GetUserId(),
                Times.Once);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByIdAsync(
                        user.Id),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.ExistsAsync(
                        invitation.BoardId,
                        user.Id),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Accept propagates an UnauthorizedAccessException
        /// when the current authenticated user cannot be resolved.
        /// </summary>
        [Fact]
        public async Task Accept_ShouldPropagateUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var request =
                new RespondToInvitationRequest
                {
                    Token = "valid-token"
                };

            _currentUserServiceMock
                .Setup(service =>
                    service.GetUserId())
                .Throws(
                    new UnauthorizedAccessException(
                        "User is not authenticated."));

            // Act
            var exception =
                await Assert.ThrowsAsync<
                    UnauthorizedAccessException>(
                    () =>
                        _controller.Accept(request));

            // Assert
            Assert.Equal(
                "User is not authenticated.",
                exception.Message);

            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.GetByTokenAsync(
                        It.IsAny<string>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<BoardMember>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Accept propagates an ArgumentException
        /// when the invitation token is empty.
        /// </summary>
        [Fact]
        public async Task Accept_ShouldPropagateArgumentException_WhenTokenIsEmpty()
        {
            // Arrange
            SetupCurrentUser(
                Guid.NewGuid());

            var request =
                new RespondToInvitationRequest
                {
                    Token = "   "
                };

            // Act
            var exception =
                await Assert.ThrowsAsync<ArgumentException>(
                    () =>
                        _controller.Accept(request));

            // Assert
            Assert.Equal(
                "Token cannot be empty.",
                exception.Message);

            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.GetByTokenAsync(
                        It.IsAny<string>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<BoardMember>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Accept propagates a KeyNotFoundException
        /// when the invitation token does not identify an invitation.
        /// </summary>
        [Fact]
        public async Task Accept_ShouldPropagateKeyNotFoundException_WhenInvitationDoesNotExist()
        {
            // Arrange
            const string token =
                "missing-token";

            SetupCurrentUser(
                Guid.NewGuid());

            var request =
                new RespondToInvitationRequest
                {
                    Token = token
                };

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(token))
                .ReturnsAsync(
                    (BoardInvitation?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () =>
                        _controller.Accept(request));

            // Assert
            Assert.Equal(
                "Invitation not found.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByIdAsync(
                        It.IsAny<Guid>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<BoardMember>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Accept propagates an UnauthorizedAccessException
        /// when the invitation belongs to another email address.
        /// </summary>
        [Fact]
        public async Task Accept_ShouldPropagateUnauthorizedAccessException_WhenInvitationBelongsToAnotherEmail()
        {
            // Arrange
            var user =
                CreateUser(
                    email: "user@example.com");

            var invitation =
                CreateInvitation(
                    email: "another@example.com");

            var request =
                new RespondToInvitationRequest
                {
                    Token = invitation.Token
                };

            SetupCurrentUser(user.Id);

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(
                        invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(
                        user.Id))
                .ReturnsAsync(user);

            // Act
            var exception =
                await Assert.ThrowsAsync<
                    UnauthorizedAccessException>(
                    () =>
                        _controller.Accept(request));

            // Assert
            Assert.Equal(
                "This invitation does not belong to your email.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.ExistsAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<BoardMember>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Accept propagates an InvalidOperationException
        /// when the invited user is already a member of the board.
        /// </summary>
        [Fact]
        public async Task Accept_ShouldPropagateInvalidOperationException_WhenUserIsAlreadyMember()
        {
            // Arrange
            var user =
                CreateUser(
                    email: "member@example.com");

            var invitation =
                CreateInvitation(
                    email: user.Email);

            var request =
                new RespondToInvitationRequest
                {
                    Token = invitation.Token
                };

            SetupCurrentUser(user.Id);

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(
                        invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(
                        user.Id))
                .ReturnsAsync(user);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.ExistsAsync(
                        invitation.BoardId,
                        user.Id))
                .ReturnsAsync(true);

            // Act
            var exception =
                await Assert.ThrowsAsync<
                    InvalidOperationException>(
                    () =>
                        _controller.Accept(request));

            // Assert
            Assert.Equal(
                "User is already a member.",
                exception.Message);

            Assert.Equal(
                InvitationStatus.Pending,
                invitation.Status);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<BoardMember>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Accept marks an expired invitation as expired,
        /// persists the state change, and propagates an exception.
        /// </summary>
        [Fact]
        public async Task Accept_ShouldExpireInvitation_WhenInvitationHasExpired()
        {
            // Arrange
            var invitation =
                CreateInvitation(
                    expiresAt:
                        DateTime.UtcNow.AddMinutes(-5));

            var request =
                new RespondToInvitationRequest
                {
                    Token = invitation.Token
                };

            SetupCurrentUser(
                Guid.NewGuid());

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(
                        invitation.Token))
                .ReturnsAsync(invitation);

            // Act
            var exception =
                await Assert.ThrowsAsync<
                    InvalidOperationException>(
                    () =>
                        _controller.Accept(request));

            // Assert
            Assert.Equal(
                "Invitation has expired.",
                exception.Message);

            Assert.Equal(
                InvitationStatus.Expired,
                invitation.Status);

            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByIdAsync(
                        It.IsAny<Guid>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<BoardMember>()),
                Times.Never);
        }

        // ============================================================
        // DECLINE
        // ============================================================

        /// <summary>
        /// Verifies that Decline returns HTTP 204 when the invited
        /// user successfully declines a pending invitation.
        /// </summary>
        [Fact]
        public async Task Decline_ShouldReturnNoContent_WhenInvitationIsValid()
        {
            // Arrange
            var user =
                CreateUser(
                    email: "member@example.com");

            var invitation =
                CreateInvitation(
                    email: user.Email);

            var request =
                new RespondToInvitationRequest
                {
                    Token = invitation.Token
                };

            SetupCurrentUser(user.Id);

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(
                        invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(
                        user.Id))
                .ReturnsAsync(user);

            // Act
            var result =
                await _controller.Decline(request);

            // Assert
            Assert.IsType<NoContentResult>(result);

            Assert.Equal(
                InvitationStatus.Declined,
                invitation.Status);

            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<BoardMember>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Decline uses the identifier of the
        /// currently authenticated user.
        /// </summary>
        [Fact]
        public async Task Decline_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var user =
                CreateUser(
                    email: "member@example.com");

            var invitation =
                CreateInvitation(
                    email: user.Email);

            var request =
                new RespondToInvitationRequest
                {
                    Token = invitation.Token
                };

            SetupCurrentUser(user.Id);

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(
                        invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(
                        user.Id))
                .ReturnsAsync(user);

            // Act
            await _controller.Decline(request);

            // Assert
            _currentUserServiceMock.Verify(
                service =>
                    service.GetUserId(),
                Times.Once);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByIdAsync(
                        user.Id),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Decline propagates an UnauthorizedAccessException
        /// when the current authenticated user cannot be resolved.
        /// </summary>
        [Fact]
        public async Task Decline_ShouldPropagateUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var request =
                new RespondToInvitationRequest
                {
                    Token = "valid-token"
                };

            _currentUserServiceMock
                .Setup(service =>
                    service.GetUserId())
                .Throws(
                    new UnauthorizedAccessException(
                        "User is not authenticated."));

            // Act
            var exception =
                await Assert.ThrowsAsync<
                    UnauthorizedAccessException>(
                    () =>
                        _controller.Decline(request));

            // Assert
            Assert.Equal(
                "User is not authenticated.",
                exception.Message);

            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.GetByTokenAsync(
                        It.IsAny<string>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Decline propagates an ArgumentException
        /// when the invitation token is empty.
        /// </summary>
        [Fact]
        public async Task Decline_ShouldPropagateArgumentException_WhenTokenIsEmpty()
        {
            // Arrange
            SetupCurrentUser(
                Guid.NewGuid());

            var request =
                new RespondToInvitationRequest
                {
                    Token = "   "
                };

            // Act
            var exception =
                await Assert.ThrowsAsync<ArgumentException>(
                    () =>
                        _controller.Decline(request));

            // Assert
            Assert.Equal(
                "Token cannot be empty.",
                exception.Message);

            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.GetByTokenAsync(
                        It.IsAny<string>()),
                Times.Never);

            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Decline propagates a KeyNotFoundException
        /// when the invitation token cannot be found.
        /// </summary>
        [Fact]
        public async Task Decline_ShouldPropagateKeyNotFoundException_WhenInvitationDoesNotExist()
        {
            // Arrange
            const string token =
                "missing-token";

            SetupCurrentUser(
                Guid.NewGuid());

            var request =
                new RespondToInvitationRequest
                {
                    Token = token
                };

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(token))
                .ReturnsAsync(
                    (BoardInvitation?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () =>
                        _controller.Decline(request));

            // Assert
            Assert.Equal(
                "Invitation not found.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByIdAsync(
                        It.IsAny<Guid>()),
                Times.Never);

            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Decline propagates an UnauthorizedAccessException
        /// when the invitation belongs to another user's email.
        /// </summary>
        [Fact]
        public async Task Decline_ShouldPropagateUnauthorizedAccessException_WhenInvitationBelongsToAnotherEmail()
        {
            // Arrange
            var user =
                CreateUser(
                    email: "user@example.com");

            var invitation =
                CreateInvitation(
                    email: "another@example.com");

            var request =
                new RespondToInvitationRequest
                {
                    Token = invitation.Token
                };

            SetupCurrentUser(user.Id);

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(
                        invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(
                        user.Id))
                .ReturnsAsync(user);

            // Act
            var exception =
                await Assert.ThrowsAsync<
                    UnauthorizedAccessException>(
                    () =>
                        _controller.Decline(request));

            // Assert
            Assert.Equal(
                "This invitation does not belong to your email.",
                exception.Message);

            Assert.Equal(
                InvitationStatus.Pending,
                invitation.Status);

            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Decline propagates an InvalidOperationException
        /// when the invitation is no longer pending.
        /// </summary>
        [Fact]
        public async Task Decline_ShouldPropagateInvalidOperationException_WhenInvitationIsNoLongerPending()
        {
            // Arrange
            var invitation =
                CreateInvitation();

            invitation.Accept();

            var request =
                new RespondToInvitationRequest
                {
                    Token = invitation.Token
                };

            SetupCurrentUser(
                Guid.NewGuid());

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(
                        invitation.Token))
                .ReturnsAsync(invitation);

            // Act
            var exception =
                await Assert.ThrowsAsync<
                    InvalidOperationException>(
                    () =>
                        _controller.Decline(request));

            // Assert
            Assert.Equal(
                "Invitation is no longer valid.",
                exception.Message);

            Assert.Equal(
                InvitationStatus.Accepted,
                invitation.Status);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByIdAsync(
                        It.IsAny<Guid>()),
                Times.Never);

            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Decline marks an expired invitation as expired,
        /// persists the state change, and propagates an exception.
        /// </summary>
        [Fact]
        public async Task Decline_ShouldExpireInvitation_WhenInvitationHasExpired()
        {
            // Arrange
            var invitation =
                CreateInvitation(
                    expiresAt:
                        DateTime.UtcNow.AddMinutes(-5));

            var request =
                new RespondToInvitationRequest
                {
                    Token = invitation.Token
                };

            SetupCurrentUser(
                Guid.NewGuid());

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(
                        invitation.Token))
                .ReturnsAsync(invitation);

            // Act
            var exception =
                await Assert.ThrowsAsync<
                    InvalidOperationException>(
                    () =>
                        _controller.Decline(request));

            // Assert
            Assert.Equal(
                "Invitation has expired.",
                exception.Message);

            Assert.Equal(
                InvitationStatus.Expired,
                invitation.Status);

            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);

            _userRepositoryMock.Verify(
                repository =>
                    repository.GetByIdAsync(
                        It.IsAny<Guid>()),
                Times.Never);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        /// <summary>
        /// Configures the current user service to return the
        /// supplied authenticated user identifier.
        /// </summary>
        private void SetupCurrentUser(Guid userId)
        {
            _currentUserServiceMock
                .Setup(service =>
                    service.GetUserId())
                .Returns(userId);
        }

        /// <summary>
        /// Creates a valid user for invitation controller tests.
        /// </summary>
        private static User CreateUser(
            string email = "member@example.com")
        {
            return new User(
                "Test User",
                "testuser",
                email,
                "password-hash");
        }

        /// <summary>
        /// Creates a pending board invitation for controller tests.
        /// </summary>
        private static BoardInvitation CreateInvitation(
            string email = "member@example.com",
            DateTime? expiresAt = null)
        {
            return new BoardInvitation(
                Guid.NewGuid(),
                Guid.NewGuid(),
                email,
                Guid.NewGuid().ToString("N"),
                expiresAt ??
                    DateTime.UtcNow.AddHours(1));
        }
    }
}