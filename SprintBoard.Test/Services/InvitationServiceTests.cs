using Moq;
using SprintBoard.Application.Interfaces;
using SprintBoard.Application.Services;
using SprintBoard.Domain.Entities;
using SprintBoard.Domain.Enums;
using Xunit;

namespace SprintBoard.Test.Services
{
    /// <summary>
    /// Contains unit tests for the <see cref="InvitationService"/>.
    /// </summary>
    public class InvitationServiceTests
    {
        private readonly Mock<IBoardInvitationRepository> _boardInvitationRepositoryMock;
        private readonly Mock<IBoardMemberRepository> _boardMemberRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly InvitationService _service;

        /// <summary>
        /// Initializes the mocked dependencies and service instance.
        /// </summary>
        public InvitationServiceTests()
        {
            _boardInvitationRepositoryMock =
                new Mock<IBoardInvitationRepository>();

            _boardMemberRepositoryMock =
                new Mock<IBoardMemberRepository>();

            _userRepositoryMock =
                new Mock<IUserRepository>();

            _service = new InvitationService(
                _boardInvitationRepositoryMock.Object,
                _boardMemberRepositoryMock.Object,
                _userRepositoryMock.Object);
        }

        // ============================================================
        // ACCEPT
        // ============================================================

        [Fact]
        public async Task AcceptAsync_ShouldThrowArgumentException_WhenTokenIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.AcceptAsync(
                    "   ",
                    userId));

            // Assert
            Assert.Equal(
                "Token cannot be empty.",
                exception.Message);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.GetByTokenAsync(
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task AcceptAsync_ShouldThrowKeyNotFoundException_WhenInvitationDoesNotExist()
        {
            // Arrange
            const string token = "invalid-token";
            var userId = Guid.NewGuid();

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(token))
                .ReturnsAsync((BoardInvitation?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => _service.AcceptAsync(
                        token,
                        userId));

            // Assert
            Assert.Equal(
                "Invitation not found.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);
        }

        [Fact]
        public async Task AcceptAsync_ShouldThrowInvalidOperationException_WhenInvitationIsNotPending()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var invitation = CreateInvitation(
                "user@example.com");

            invitation.Decline();

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(invitation.Token))
                .ReturnsAsync(invitation);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.AcceptAsync(
                        invitation.Token,
                        userId));

            // Assert
            Assert.Equal(
                "Invitation is no longer valid.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);
        }

        [Fact]
        public async Task AcceptAsync_ShouldExpireInvitationAndThrow_WhenInvitationHasExpired()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var invitation = CreateExpiredInvitation(
                "user@example.com");

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(invitation.Token))
                .ReturnsAsync(invitation);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.AcceptAsync(
                        invitation.Token,
                        userId));

            // Assert
            Assert.Equal(
                "Invitation has expired.",
                exception.Message);

            Assert.Equal(
                InvitationStatus.Expired,
                invitation.Status);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);

            _userRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);
        }

        [Fact]
        public async Task AcceptAsync_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var invitation = CreateInvitation(
                "user@example.com");

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => _service.AcceptAsync(
                        invitation.Token,
                        userId));

            // Assert
            Assert.Equal(
                "User not found.",
                exception.Message);

            Assert.Equal(
                InvitationStatus.Pending,
                invitation.Status);

            _boardMemberRepositoryMock.Verify(
                repository => repository.ExistsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);
        }

        [Fact]
        public async Task AcceptAsync_ShouldThrowUnauthorizedAccessException_WhenEmailDoesNotMatch()
        {
            // Arrange
            var invitation = CreateInvitation(
                "invited@example.com");

            var user = CreateUser(
                "other@example.com");

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => _service.AcceptAsync(
                        invitation.Token,
                        user.Id));

            // Assert
            Assert.Equal(
                "This invitation does not belong to your email.",
                exception.Message);

            Assert.Equal(
                InvitationStatus.Pending,
                invitation.Status);

            _boardMemberRepositoryMock.Verify(
                repository => repository.ExistsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);
        }

        [Fact]
        public async Task AcceptAsync_ShouldThrowInvalidOperationException_WhenUserIsAlreadyMember()
        {
            // Arrange
            var invitation = CreateInvitation(
                "member@example.com");

            var user = CreateUser(
                "member@example.com");

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _boardMemberRepositoryMock
                .Setup(repository => repository.ExistsAsync(
                    invitation.BoardId,
                    user.Id))
                .ReturnsAsync(true);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.AcceptAsync(
                        invitation.Token,
                        user.Id));

            // Assert
            Assert.Equal(
                "User is already a member.",
                exception.Message);

            Assert.Equal(
                InvitationStatus.Pending,
                invitation.Status);

            _boardMemberRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task AcceptAsync_ShouldAddMemberAcceptInvitationAndSave()
        {
            // Arrange
            var invitation = CreateInvitation(
                "member@example.com");

            var user = CreateUser(
                "member@example.com");

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _boardMemberRepositoryMock
                .Setup(repository => repository.ExistsAsync(
                    invitation.BoardId,
                    user.Id))
                .ReturnsAsync(false);

            // Act
            await _service.AcceptAsync(
                invitation.Token,
                user.Id);

            // Assert
            Assert.Equal(
                InvitationStatus.Accepted,
                invitation.Status);

            _boardMemberRepositoryMock.Verify(
                repository => repository.ExistsAsync(
                    invitation.BoardId,
                    user.Id),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<BoardMember>(member =>
                        member.BoardId == invitation.BoardId &&
                        member.UserId == user.Id &&
                        member.Role == BoardRole.Member)),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        // ============================================================
        // DECLINE
        // ============================================================

        [Fact]
        public async Task DeclineAsync_ShouldThrowArgumentException_WhenTokenIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.DeclineAsync(
                    "   ",
                    userId));

            // Assert
            Assert.Equal(
                "Token cannot be empty.",
                exception.Message);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.GetByTokenAsync(
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task DeclineAsync_ShouldThrowKeyNotFoundException_WhenInvitationDoesNotExist()
        {
            // Arrange
            const string token = "invalid-token";
            var userId = Guid.NewGuid();

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(token))
                .ReturnsAsync((BoardInvitation?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => _service.DeclineAsync(
                        token,
                        userId));

            // Assert
            Assert.Equal(
                "Invitation not found.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>()),
                Times.Never);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task DeclineAsync_ShouldThrowInvalidOperationException_WhenInvitationIsNotPending()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var invitation = CreateInvitation(
                "user@example.com");

            invitation.Accept();

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(invitation.Token))
                .ReturnsAsync(invitation);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.DeclineAsync(
                        invitation.Token,
                        userId));

            // Assert
            Assert.Equal(
                "Invitation is no longer valid.",
                exception.Message);

            _userRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>()),
                Times.Never);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task DeclineAsync_ShouldExpireInvitationAndThrow_WhenInvitationHasExpired()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var invitation = CreateExpiredInvitation(
                "user@example.com");

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(invitation.Token))
                .ReturnsAsync(invitation);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.DeclineAsync(
                        invitation.Token,
                        userId));

            // Assert
            Assert.Equal(
                "Invitation has expired.",
                exception.Message);

            Assert.Equal(
                InvitationStatus.Expired,
                invitation.Status);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);

            _userRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task DeclineAsync_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var invitation = CreateInvitation(
                "user@example.com");

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => _service.DeclineAsync(
                        invitation.Token,
                        userId));

            // Assert
            Assert.Equal(
                "User not found.",
                exception.Message);

            Assert.Equal(
                InvitationStatus.Pending,
                invitation.Status);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task DeclineAsync_ShouldThrowUnauthorizedAccessException_WhenEmailDoesNotMatch()
        {
            // Arrange
            var invitation = CreateInvitation(
                "invited@example.com");

            var user = CreateUser(
                "other@example.com");

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => _service.DeclineAsync(
                        invitation.Token,
                        user.Id));

            // Assert
            Assert.Equal(
                "This invitation does not belong to your email.",
                exception.Message);

            Assert.Equal(
                InvitationStatus.Pending,
                invitation.Status);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task DeclineAsync_ShouldDeclineInvitationAndSave()
        {
            // Arrange
            var invitation = CreateInvitation(
                "member@example.com");

            var user = CreateUser(
                "member@example.com");

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.GetByTokenAsync(invitation.Token))
                .ReturnsAsync(invitation);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            await _service.DeclineAsync(
                invitation.Token,
                user.Id);

            // Assert
            Assert.Equal(
                InvitationStatus.Declined,
                invitation.Status);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        /// <summary>
        /// Creates a valid pending invitation for unit tests.
        /// </summary>
        private static BoardInvitation CreateInvitation(string email)
        {
            return new BoardInvitation(
                Guid.NewGuid(),
                Guid.NewGuid(),
                email,
                Guid.NewGuid().ToString("N"),
                DateTime.UtcNow.AddDays(7));
        }

        /// <summary>
        /// Creates an expired pending invitation for unit tests.
        /// </summary>
        private static BoardInvitation CreateExpiredInvitation(string email)
        {
            return new BoardInvitation(
                Guid.NewGuid(),
                Guid.NewGuid(),
                email,
                Guid.NewGuid().ToString("N"),
                DateTime.UtcNow.AddDays(-1));
        }

        /// <summary>
        /// Creates a user with the supplied email address.
        /// </summary>
        private static User CreateUser(string email)
        {
            return new User(
                "Test User",
                $"user-{Guid.NewGuid():N}",
                email,
                "password-hash");
        }
    }
}