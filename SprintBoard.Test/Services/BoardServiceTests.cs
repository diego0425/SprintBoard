using Moq;
using SprintBoard.Application.DTOs.Board;
using SprintBoard.Application.Exceptions;
using SprintBoard.Application.Interfaces;
using SprintBoard.Application.Services;
using SprintBoard.Domain.Entities;
using SprintBoard.Domain.Enums;
using Xunit;

namespace SprintBoard.Test.Services
{
    /// <summary>
    /// Contains unit tests for the <see cref="BoardService"/>.
    /// </summary>
    public class BoardServiceTests
    {
        private readonly Mock<IBoardRepository> _boardRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IBoardMemberRepository> _boardMemberRepositoryMock;
        private readonly Mock<IBoardInvitationRepository> _boardInvitationRepositoryMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IMembershipAuthorizationService> _membershipAuthorizationServiceMock;
        private readonly Mock<IInvitationLinkBuilder> _invitationLinkBuilderMock;

        private readonly BoardService _service;

        /// <summary>
        /// Initializes the test dependencies and service instance.
        /// </summary>
        public BoardServiceTests()
        {
            _boardRepositoryMock = new Mock<IBoardRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _boardMemberRepositoryMock = new Mock<IBoardMemberRepository>();
            _boardInvitationRepositoryMock = new Mock<IBoardInvitationRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _membershipAuthorizationServiceMock =
                new Mock<IMembershipAuthorizationService>();
            _invitationLinkBuilderMock =
                new Mock<IInvitationLinkBuilder>();

            _service = new BoardService(
                _boardRepositoryMock.Object,
                _userRepositoryMock.Object,
                _boardMemberRepositoryMock.Object,
                _boardInvitationRepositoryMock.Object,
                _emailServiceMock.Object,
                _membershipAuthorizationServiceMock.Object,
                _invitationLinkBuilderMock.Object);
        }

        // ============================================================
        // CREATE
        // ============================================================

        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentException_WhenNameIsEmpty()
        {
            // Arrange
            var ownerId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync("   ", ownerId));

            // Assert
            Assert.Equal("Board name cannot be empty.", exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<Board>()),
                Times.Never);

            _boardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<BoardMember>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentException_WhenOwnerIdIsEmpty()
        {
            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(
                    "Test board",
                    Guid.Empty));

            // Assert
            Assert.Equal("OwnerId cannot be empty.", exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<Board>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<BoardMember>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateBoardAndOwnerMembership()
        {
            // Arrange
            var ownerId = Guid.NewGuid();

            // Act
            var result = await _service.CreateAsync(
                "   Sprint Board   ",
                ownerId);

            // Assert
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("Sprint Board", result.Name);
            Assert.Equal(ownerId, result.OwnerId);
            Assert.NotEqual(default, result.CreatedAt);

            _boardRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<Board>(board =>
                        board.Name == "Sprint Board" &&
                        board.OwnerId == ownerId)),
                Times.Once);

            _boardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<BoardMember>(member =>
                        member.BoardId == result.Id &&
                        member.UserId == ownerId &&
                        member.Role == BoardRole.Owner)),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        // ============================================================
        // GET BY ID - WITHOUT MEMBERSHIP CHECK
        // ============================================================

        [Fact]
        public async Task GetByIdAsync_ShouldThrowArgumentException_WhenBoardIdIsEmpty()
        {
            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetByIdAsync(Guid.Empty));

            // Assert
            Assert.Equal("BoardId cannot be empty.", exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrowKeyNotFoundException_WhenBoardDoesNotExist()
        {
            // Arrange
            var boardId = Guid.NewGuid();

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.GetByIdAsync(boardId));

            // Assert
            Assert.Equal("Board not found.", exception.Message);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnBoard_WhenBoardExists()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();

            var board = new Board(
                "Backend Project",
                ownerId);

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            // Act
            var result = await _service.GetByIdAsync(boardId);

            // Assert
            Assert.Equal(board.Id, result.Id);
            Assert.Equal(board.Name, result.Name);
            Assert.Equal(board.OwnerId, result.OwnerId);
            Assert.Equal(board.CreatedAt, result.CreatedAt);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(boardId),
                Times.Once);
        }

        // ============================================================
        // EXISTS
        // ============================================================

        [Fact]
        public async Task ExistsAsync_ShouldThrowArgumentException_WhenBoardIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ExistsAsync(
                    Guid.Empty,
                    userId));

            // Assert
            Assert.Equal("BoardId cannot be empty.", exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task ExistsAsync_ShouldThrowArgumentException_WhenUserIdIsEmpty()
        {
            // Arrange
            var boardId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ExistsAsync(
                    boardId,
                    Guid.Empty));

            // Assert
            Assert.Equal("UserId cannot be empty.", exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task ExistsAsync_ShouldThrowKeyNotFoundException_WhenBoardDoesNotExist()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.ExistsAsync(
                    boardId,
                    userId));

            // Assert
            Assert.Equal("Board not found.", exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository => repository.ExistsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenUserIsMember()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();

            var board = new Board(
                "Test board",
                ownerId);

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _boardMemberRepositoryMock
                .Setup(repository => repository.ExistsAsync(
                    boardId,
                    userId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ExistsAsync(
                boardId,
                userId);

            // Assert
            Assert.True(result);

            _boardMemberRepositoryMock.Verify(
                repository => repository.ExistsAsync(
                    boardId,
                    userId),
                Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenUserIsNotMember()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();

            var board = new Board(
                "Test board",
                ownerId);

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _boardMemberRepositoryMock
                .Setup(repository => repository.ExistsAsync(
                    boardId,
                    userId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.ExistsAsync(
                boardId,
                userId);

            // Assert
            Assert.False(result);

            _boardMemberRepositoryMock.Verify(
                repository => repository.ExistsAsync(
                    boardId,
                    userId),
                Times.Once);
        }

        // ============================================================
        // GET BY USER
        // ============================================================

        [Fact]
        public async Task GetByUserAsync_ShouldThrowArgumentException_WhenUserIdIsEmpty()
        {
            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetByUserAsync(Guid.Empty));

            // Assert
            Assert.Equal("UserId cannot be empty.", exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByUserMembershipAsync(
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task GetByUserAsync_ShouldReturnUserBoards()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var firstBoard = new Board(
                "First board",
                userId);

            var secondBoard = new Board(
                "Second board",
                Guid.NewGuid());

            var boards = new List<Board>
            {
                firstBoard,
                secondBoard
            };

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByUserMembershipAsync(userId))
                .ReturnsAsync(boards);

            // Act
            var result = (
                await _service.GetByUserAsync(userId))
                .ToList();

            // Assert
            Assert.Equal(2, result.Count);

            Assert.Equal(firstBoard.Id, result[0].Id);
            Assert.Equal("First board", result[0].Name);
            Assert.Equal(firstBoard.OwnerId, result[0].OwnerId);

            Assert.Equal(secondBoard.Id, result[1].Id);
            Assert.Equal("Second board", result[1].Name);
            Assert.Equal(secondBoard.OwnerId, result[1].OwnerId);

            _boardRepositoryMock.Verify(
                repository =>
                    repository.GetByUserMembershipAsync(userId),
                Times.Once);
        }

        // ============================================================
        // GET BY ID - WITH MEMBERSHIP CHECK
        // ============================================================

        [Fact]
        public async Task GetByIdWithUserAsync_ShouldThrowArgumentException_WhenBoardIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetByIdAsync(
                    Guid.Empty,
                    userId));

            // Assert
            Assert.Equal("BoardId cannot be empty.", exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardMemberAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task GetByIdWithUserAsync_ShouldThrowKeyNotFoundException_WhenBoardDoesNotExist()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.GetByIdAsync(
                    boardId,
                    userId));

            // Assert
            Assert.Equal("Board not found.", exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardMemberAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task GetByIdWithUserAsync_ShouldThrowForbiddenAccessException_WhenAccessIsDenied()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board = new Board(
                "Private board",
                Guid.NewGuid());

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardMemberAsync(
                    boardId,
                    userId))
                .ThrowsAsync(
                    new ForbiddenAccessException("Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.GetByIdAsync(
                    boardId,
                    userId));

            // Assert
            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardMemberAsync(
                    boardId,
                    userId),
                Times.Once);
        }

        [Fact]
        public async Task GetByIdWithUserAsync_ShouldReturnBoard_WhenUserIsMember()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board = new Board(
                "Authorized board",
                Guid.NewGuid());

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardMemberAsync(
                    boardId,
                    userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.GetByIdAsync(
                boardId,
                userId);

            // Assert
            Assert.Equal(board.Id, result.Id);
            Assert.Equal(board.Name, result.Name);
            Assert.Equal(board.OwnerId, result.OwnerId);
            Assert.Equal(board.CreatedAt, result.CreatedAt);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardMemberAsync(
                    boardId,
                    userId),
                Times.Once);
        }

        // ============================================================
        // REMOVE
        // ============================================================

        [Fact]
        public async Task RemoveAsync_ShouldThrowArgumentException_WhenBoardIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RemoveAsync(
                    Guid.Empty,
                    userId));

            // Assert
            Assert.Equal("BoardId cannot be empty.", exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.RemoveAsync(
                    It.IsAny<Board>()),
                Times.Never);
        }

        [Fact]
        public async Task RemoveAsync_ShouldThrowKeyNotFoundException_WhenBoardDoesNotExist()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.RemoveAsync(
                    boardId,
                    userId));

            // Assert
            Assert.Equal("Board not found.", exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardOwnerAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);

            _boardRepositoryMock.Verify(
                repository => repository.RemoveAsync(
                    It.IsAny<Board>()),
                Times.Never);
        }

        [Fact]
        public async Task RemoveAsync_ShouldNotRemoveBoard_WhenUserIsNotOwner()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board = new Board(
                "Protected board",
                Guid.NewGuid());

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerAsync(
                    boardId,
                    userId))
                .ThrowsAsync(
                    new ForbiddenAccessException("Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.RemoveAsync(
                    boardId,
                    userId));

            // Assert
            _boardRepositoryMock.Verify(
                repository => repository.RemoveAsync(
                    It.IsAny<Board>()),
                Times.Never);

            _boardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task RemoveAsync_ShouldRemoveBoard_WhenUserIsOwner()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board = new Board(
                "Board to remove",
                userId);

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerAsync(
                    boardId,
                    userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.RemoveAsync(
                boardId,
                userId);

            // Assert
            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardOwnerAsync(
                    boardId,
                    userId),
                Times.Once);

            _boardRepositoryMock.Verify(
                repository => repository.RemoveAsync(board),
                Times.Once);

            _boardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        // ============================================================
        // UPDATE
        // ============================================================

        [Fact]
        public async Task UpdateAsync_ShouldThrowArgumentException_WhenBoardIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var request = new UpdateBoardRequest
            {
                Name = "Updated board"
            };

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateAsync(
                    Guid.Empty,
                    userId,
                    request));

            // Assert
            Assert.Equal("BoardId cannot be empty.", exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowKeyNotFoundException_WhenBoardDoesNotExist()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var request = new UpdateBoardRequest
            {
                Name = "Updated board"
            };

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateAsync(
                    boardId,
                    userId,
                    request));

            // Assert
            Assert.Equal("Board not found.", exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardOwnerAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);

            _boardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldNotUpdateBoard_WhenUserIsNotOwner()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board = new Board(
                "Original name",
                Guid.NewGuid());

            var request = new UpdateBoardRequest
            {
                Name = "Unauthorized name"
            };

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerAsync(
                    boardId,
                    userId))
                .ThrowsAsync(
                    new ForbiddenAccessException("Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.UpdateAsync(
                    boardId,
                    userId,
                    request));

            // Assert
            Assert.Equal("Original name", board.Name);

            _boardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateAndTrimBoardName()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board = new Board(
                "Original name",
                userId);

            var request = new UpdateBoardRequest
            {
                Name = "   Updated board   "
            };

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerAsync(
                    boardId,
                    userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAsync(
                boardId,
                userId,
                request);

            // Assert
            Assert.Equal("Updated board", board.Name);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardOwnerAsync(
                    boardId,
                    userId),
                Times.Once);

            _boardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldKeepCurrentName_WhenNameIsBlank()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board = new Board(
                "Original board",
                userId);

            var request = new UpdateBoardRequest
            {
                Name = "   "
            };

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerAsync(
                    boardId,
                    userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAsync(
                boardId,
                userId,
                request);

            // Assert
            Assert.Equal("Original board", board.Name);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardOwnerAsync(
                    boardId,
                    userId),
                Times.Once);

            _boardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        // ============================================================
        // CREATE INVITATION
        // ============================================================

        [Fact]
        public async Task CreateInvitationAsync_ShouldThrowArgumentException_WhenBoardIdIsEmpty()
        {
            var requesterUserId = Guid.NewGuid();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateInvitationAsync(
                    Guid.Empty,
                    requesterUserId,
                    "user@example.com"));

            Assert.Equal("BoardId cannot be empty.", exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateInvitationAsync_ShouldThrowArgumentException_WhenRequesterIdIsEmpty()
        {
            var boardId = Guid.NewGuid();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateInvitationAsync(
                    boardId,
                    Guid.Empty,
                    "user@example.com"));

            Assert.Equal(
                "Requester user id cannot be empty.",
                exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateInvitationAsync_ShouldThrowArgumentException_WhenEmailIsEmpty()
        {
            var boardId = Guid.NewGuid();
            var requesterUserId = Guid.NewGuid();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateInvitationAsync(
                    boardId,
                    requesterUserId,
                    "   "));

            Assert.Equal("Email cannot be empty.", exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateInvitationAsync_ShouldThrowKeyNotFoundException_WhenBoardDoesNotExist()
        {
            var boardId = Guid.NewGuid();
            var requesterUserId = Guid.NewGuid();

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.CreateInvitationAsync(
                    boardId,
                    requesterUserId,
                    "user@example.com"));

            Assert.Equal("Board not found.", exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardOwnerOrAdminAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<BoardInvitation>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateInvitationAsync_ShouldNotCreateInvitation_WhenAccessIsForbidden()
        {
            var boardId = Guid.NewGuid();
            var requesterUserId = Guid.NewGuid();

            var board = new Board(
                "Private board",
                requesterUserId);

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerOrAdminAsync(
                    boardId,
                    requesterUserId))
                .ThrowsAsync(
                    new ForbiddenAccessException("Access denied."));

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.CreateInvitationAsync(
                    boardId,
                    requesterUserId,
                    "user@example.com"));

            _userRepositoryMock.Verify(
                repository => repository.GetByEmailAsync(
                    It.IsAny<string>()),
                Times.Never);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<BoardInvitation>()),
                Times.Never);

            _emailServiceMock.Verify(
                service => service.SendBoardInvitationAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateInvitationAsync_ShouldThrowInvalidOperationException_WhenUserIsAlreadyMember()
        {
            var boardId = Guid.NewGuid();
            var requesterUserId = Guid.NewGuid();

            var board = new Board(
                "Team board",
                requesterUserId);

            var invitedUser = new User(
                "Existing User",
                "existinguser",
                "existing@example.com",
                "hash");

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerOrAdminAsync(
                    boardId,
                    requesterUserId))
                .Returns(Task.CompletedTask);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("existing@example.com"))
                .ReturnsAsync(invitedUser);

            _boardMemberRepositoryMock
                .Setup(repository => repository.ExistsAsync(
                    boardId,
                    invitedUser.Id))
                .ReturnsAsync(true);

            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.CreateInvitationAsync(
                        boardId,
                        requesterUserId,
                        " Existing@Example.com "));

            Assert.Equal(
                "User is already a member of this board.",
                exception.Message);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.ExistsPendingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>()),
                Times.Never);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<BoardInvitation>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateInvitationAsync_ShouldThrowInvalidOperationException_WhenPendingInvitationAlreadyExists()
        {
            var boardId = Guid.NewGuid();
            var requesterUserId = Guid.NewGuid();

            var board = new Board(
                "Team board",
                requesterUserId);

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerOrAdminAsync(
                    boardId,
                    requesterUserId))
                .Returns(Task.CompletedTask);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("user@example.com"))
                .ReturnsAsync((User?)null);

            _boardInvitationRepositoryMock
                .Setup(repository => repository.ExistsPendingAsync(
                    boardId,
                    "user@example.com"))
                .ReturnsAsync(true);

            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.CreateInvitationAsync(
                        boardId,
                        requesterUserId,
                        " USER@EXAMPLE.COM "));

            Assert.Equal(
                "There is already a pending invitation for this email.",
                exception.Message);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<BoardInvitation>()),
                Times.Never);

            _emailServiceMock.Verify(
                service => service.SendBoardInvitationAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateInvitationAsync_ShouldCreateSaveAndSendInvitation()
        {
            var boardId = Guid.NewGuid();
            var requesterUserId = Guid.NewGuid();

            var board = new Board(
                "Team Board",
                requesterUserId);

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerOrAdminAsync(
                    boardId,
                    requesterUserId))
                .Returns(Task.CompletedTask);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync("candidate@example.com"))
                .ReturnsAsync((User?)null);

            _boardInvitationRepositoryMock
                .Setup(repository => repository.ExistsPendingAsync(
                    boardId,
                    "candidate@example.com"))
                .ReturnsAsync(false);

            _invitationLinkBuilderMock
                .Setup(builder =>
                    builder.BuildAcceptInvitationLink(
                        It.IsAny<string>()))
                .Returns<string>(
                    token => $"https://test/accept/{token}");

            _invitationLinkBuilderMock
                .Setup(builder =>
                    builder.BuildDeclineInvitationLink(
                        It.IsAny<string>()))
                .Returns<string>(
                    token => $"https://test/decline/{token}");

            var result = await _service.CreateInvitationAsync(
                boardId,
                requesterUserId,
                " Candidate@Example.com ");

            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(boardId, result.BoardId);
            Assert.Equal("candidate@example.com", result.Email);

            Assert.False(
                string.IsNullOrWhiteSpace(result.Token));

            Assert.Equal(64, result.Token.Length);

            Assert.All(
                result.Token,
                character =>
                    Assert.True(Uri.IsHexDigit(character)));

            Assert.True(
                result.ExpiresAt > DateTime.UtcNow.AddDays(6));

            _boardInvitationRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<BoardInvitation>(invitation =>
                        invitation.BoardId == boardId &&
                        invitation.InvitedByUserId == requesterUserId &&
                        invitation.Email == "candidate@example.com" &&
                        invitation.Token == result.Token)),
                Times.Once);

            _boardInvitationRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);

            _invitationLinkBuilderMock.Verify(
                builder => builder.BuildAcceptInvitationLink(
                    result.Token),
                Times.Once);

            _invitationLinkBuilderMock.Verify(
                builder => builder.BuildDeclineInvitationLink(
                    result.Token),
                Times.Once);

            _emailServiceMock.Verify(
                service => service.SendBoardInvitationAsync(
                    "candidate@example.com",
                    "Team Board",
                    $"https://test/accept/{result.Token}",
                    $"https://test/decline/{result.Token}"),
                Times.Once);
        }

        // ============================================================
        // CHANGE MEMBER ROLE
        // ============================================================

        [Fact]
        public async Task ChangeMemberRoleAsync_ShouldNotChangeRole_WhenRequesterIsNotOwner()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerAsync(
                    boardId,
                    requesterId))
                .ThrowsAsync(
                    new ForbiddenAccessException("Access denied."));

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.ChangeMemberRoleAsync(
                    boardId,
                    requesterId,
                    memberId,
                    (int)BoardRole.Admin));

            _boardMemberRepositoryMock.Verify(
                repository => repository.GetMemberAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ChangeMemberRoleAsync_ShouldThrowKeyNotFoundException_WhenMemberDoesNotExist()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerAsync(
                    boardId,
                    requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    memberId))
                .ReturnsAsync((BoardMember?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.ChangeMemberRoleAsync(
                    boardId,
                    requesterId,
                    memberId,
                    (int)BoardRole.Admin));

            Assert.Equal("Member not found.", exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ChangeMemberRoleAsync_ShouldThrowInvalidOperationException_WhenTargetIsOwner()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();

            var ownerMembership = new BoardMember(
                boardId,
                ownerId,
                BoardRole.Owner);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerAsync(
                    boardId,
                    requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    ownerId))
                .ReturnsAsync(ownerMembership);

            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.ChangeMemberRoleAsync(
                        boardId,
                        requesterId,
                        ownerId,
                        (int)BoardRole.Admin));

            Assert.Equal(
                "Cannot change the owner's role.",
                exception.Message);

            Assert.Equal(
                BoardRole.Owner,
                ownerMembership.Role);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ChangeMemberRoleAsync_ShouldThrowInvalidOperationException_WhenAssigningOwnerRole()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var membership = new BoardMember(
                boardId,
                memberId,
                BoardRole.Member);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerAsync(
                    boardId,
                    requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    memberId))
                .ReturnsAsync(membership);

            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.ChangeMemberRoleAsync(
                        boardId,
                        requesterId,
                        memberId,
                        (int)BoardRole.Owner));

            Assert.Equal(
                "The Owner role cannot be assigned to another member.",
                exception.Message);

            Assert.Equal(
                BoardRole.Member,
                membership.Role);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ChangeMemberRoleAsync_ShouldThrowArgumentException_WhenRoleIsInvalid()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var membership = new BoardMember(
                boardId,
                memberId,
                BoardRole.Member);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerAsync(
                    boardId,
                    requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    memberId))
                .ReturnsAsync(membership);

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ChangeMemberRoleAsync(
                    boardId,
                    requesterId,
                    memberId,
                    999));

            Assert.Equal(
                BoardRole.Member,
                membership.Role);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task ChangeMemberRoleAsync_ShouldChangeMemberRoleAndSave()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var membership = new BoardMember(
                boardId,
                memberId,
                BoardRole.Member);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerAsync(
                    boardId,
                    requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    memberId))
                .ReturnsAsync(membership);

            await _service.ChangeMemberRoleAsync(
                boardId,
                requesterId,
                memberId,
                (int)BoardRole.Admin);

            Assert.Equal(
                BoardRole.Admin,
                membership.Role);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardOwnerAsync(
                    boardId,
                    requesterId),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        // ============================================================
        // REMOVE MEMBER
        // ============================================================

        [Fact]
        public async Task RemoveMemberAsync_ShouldThrowArgumentException_WhenBoardIdIsEmpty()
        {
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RemoveMemberAsync(
                    Guid.Empty,
                    Guid.NewGuid(),
                    Guid.NewGuid()));

            Assert.Equal(
                "BoardId cannot be empty.",
                exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldThrowArgumentException_WhenRequesterIdIsEmpty()
        {
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RemoveMemberAsync(
                    Guid.NewGuid(),
                    Guid.Empty,
                    Guid.NewGuid()));

            Assert.Equal(
                "Requester user id cannot be empty.",
                exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldThrowArgumentException_WhenMemberIdIsEmpty()
        {
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RemoveMemberAsync(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.Empty));

            Assert.Equal(
                "Member user id cannot be empty.",
                exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldThrowKeyNotFoundException_WhenBoardDoesNotExist()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.RemoveMemberAsync(
                    boardId,
                    requesterId,
                    memberId));

            Assert.Equal(
                "Board not found.",
                exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardOwnerOrAdminAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldNotRemoveMember_WhenAccessIsForbidden()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var board = new Board(
                "Protected board",
                Guid.NewGuid());

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerOrAdminAsync(
                    boardId,
                    requesterId))
                .ThrowsAsync(
                    new ForbiddenAccessException("Access denied."));

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.RemoveMemberAsync(
                    boardId,
                    requesterId,
                    memberId));

            _boardMemberRepositoryMock.Verify(
                repository => repository.GetMemberAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository => repository.RemoveAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldThrowKeyNotFoundException_WhenRequesterMembershipDoesNotExist()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var board = new Board(
                "Board",
                Guid.NewGuid());

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerOrAdminAsync(
                    boardId,
                    requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    requesterId))
                .ReturnsAsync((BoardMember?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.RemoveMemberAsync(
                    boardId,
                    requesterId,
                    memberId));

            Assert.Equal(
                "Requester membership not found.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository => repository.GetMemberAsync(
                    boardId,
                    memberId),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository => repository.RemoveAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldThrowKeyNotFoundException_WhenTargetMemberDoesNotExist()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var board = new Board(
                "Board",
                requesterId);

            var requesterMembership = new BoardMember(
                boardId,
                requesterId,
                BoardRole.Owner);

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerOrAdminAsync(
                    boardId,
                    requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    requesterId))
                .ReturnsAsync(requesterMembership);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    memberId))
                .ReturnsAsync((BoardMember?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.RemoveMemberAsync(
                    boardId,
                    requesterId,
                    memberId));

            Assert.Equal(
                "Member not found.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository => repository.RemoveAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldThrowInvalidOperationException_WhenTargetIsOwner()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();

            var board = new Board(
                "Board",
                ownerId);

            var requesterMembership = new BoardMember(
                boardId,
                requesterId,
                BoardRole.Admin);

            var ownerMembership = new BoardMember(
                boardId,
                ownerId,
                BoardRole.Owner);

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerOrAdminAsync(
                    boardId,
                    requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    requesterId))
                .ReturnsAsync(requesterMembership);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    ownerId))
                .ReturnsAsync(ownerMembership);

            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.RemoveMemberAsync(
                        boardId,
                        requesterId,
                        ownerId));

            Assert.Equal(
                "The board owner cannot be removed.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository => repository.RemoveAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldThrowForbiddenAccessException_WhenAdminTriesToRemoveAdmin()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var board = new Board(
                "Board",
                Guid.NewGuid());

            var requesterMembership = new BoardMember(
                boardId,
                requesterId,
                BoardRole.Admin);

            var memberMembership = new BoardMember(
                boardId,
                memberId,
                BoardRole.Admin);

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerOrAdminAsync(
                    boardId,
                    requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    requesterId))
                .ReturnsAsync(requesterMembership);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    memberId))
                .ReturnsAsync(memberMembership);

            var exception =
                await Assert.ThrowsAsync<ForbiddenAccessException>(
                    () => _service.RemoveMemberAsync(
                        boardId,
                        requesterId,
                        memberId));

            Assert.Equal(
                "Administrators can only remove members with the Member role.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository => repository.RemoveAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldAllowAdminToRemoveMember()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var board = new Board(
                "Board",
                Guid.NewGuid());

            var requesterMembership = new BoardMember(
                boardId,
                requesterId,
                BoardRole.Admin);

            var memberMembership = new BoardMember(
                boardId,
                memberId,
                BoardRole.Member);

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerOrAdminAsync(
                    boardId,
                    requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    requesterId))
                .ReturnsAsync(requesterMembership);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    memberId))
                .ReturnsAsync(memberMembership);

            await _service.RemoveMemberAsync(
                boardId,
                requesterId,
                memberId);

            _boardMemberRepositoryMock.Verify(
                repository => repository.RemoveAsync(
                    memberMembership),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldAllowOwnerToRemoveAdmin()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var board = new Board(
                "Board",
                requesterId);

            var requesterMembership = new BoardMember(
                boardId,
                requesterId,
                BoardRole.Owner);

            var memberMembership = new BoardMember(
                boardId,
                memberId,
                BoardRole.Admin);

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardOwnerOrAdminAsync(
                    boardId,
                    requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    requesterId))
                .ReturnsAsync(requesterMembership);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    memberId))
                .ReturnsAsync(memberMembership);

            await _service.RemoveMemberAsync(
                boardId,
                requesterId,
                memberId);

            _boardMemberRepositoryMock.Verify(
                repository => repository.RemoveAsync(
                    memberMembership),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        // ============================================================
        // LEAVE BOARD
        // ============================================================

        [Fact]
        public async Task LeaveBoardAsync_ShouldThrowArgumentException_WhenBoardIdIsEmpty()
        {
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.LeaveBoardAsync(
                    Guid.Empty,
                    Guid.NewGuid()));

            Assert.Equal(
                "BoardId cannot be empty.",
                exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task LeaveBoardAsync_ShouldThrowArgumentException_WhenUserIdIsEmpty()
        {
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.LeaveBoardAsync(
                    Guid.NewGuid(),
                    Guid.Empty));

            Assert.Equal(
                "UserId cannot be empty.",
                exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task LeaveBoardAsync_ShouldThrowKeyNotFoundException_WhenBoardDoesNotExist()
        {
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.LeaveBoardAsync(
                    boardId,
                    userId));

            Assert.Equal(
                "Board not found.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository => repository.GetMemberAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task LeaveBoardAsync_ShouldThrowKeyNotFoundException_WhenMembershipDoesNotExist()
        {
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board = new Board(
                "Board",
                Guid.NewGuid());

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    userId))
                .ReturnsAsync((BoardMember?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.LeaveBoardAsync(
                    boardId,
                    userId));

            Assert.Equal(
                "Board membership not found.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository => repository.RemoveAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);
        }

        [Fact]
        public async Task LeaveBoardAsync_ShouldThrowInvalidOperationException_WhenOwnerTriesToLeave()
        {
            var boardId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();

            var board = new Board(
                "Board",
                ownerId);

            var membership = new BoardMember(
                boardId,
                ownerId,
                BoardRole.Owner);

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    ownerId))
                .ReturnsAsync(membership);

            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _service.LeaveBoardAsync(
                        boardId,
                        ownerId));

            Assert.Equal(
                "The board owner cannot leave the board. Delete the board instead.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository => repository.RemoveAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task LeaveBoardAsync_ShouldRemoveMembershipAndSave_WhenMemberLeaves()
        {
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board = new Board(
                "Board",
                Guid.NewGuid());

            var membership = new BoardMember(
                boardId,
                userId,
                BoardRole.Member);

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMemberAsync(
                    boardId,
                    userId))
                .ReturnsAsync(membership);

            await _service.LeaveBoardAsync(
                boardId,
                userId);

            _boardMemberRepositoryMock.Verify(
                repository => repository.RemoveAsync(
                    membership),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        // ============================================================
        // GET BOARD MEMBERS
        // ============================================================

        [Fact]
        public async Task GetBoardMembersAsync_ShouldNotReturnMembers_WhenAccessIsForbidden()
        {
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardMemberAsync(
                    boardId,
                    userId))
                .ThrowsAsync(
                    new ForbiddenAccessException("Access denied."));

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.GetBoardMembersAsync(
                    boardId,
                    userId));

            _boardMemberRepositoryMock.Verify(
                repository => repository.GetMembersAsync(
                    It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task GetBoardMembersAsync_ShouldReturnMappedMembers()
        {
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();

            var firstUser = new User(
                "Alice Doe",
                "alice",
                "alice@example.com",
                "hash");

            firstUser.UpdateProfileImage(
                "https://example.com/alice.jpg");

            var secondUser = new User(
                "Bob Doe",
                "bob",
                "bob@example.com",
                "hash");

            var firstMembership = new BoardMember(
                boardId,
                firstUser.Id,
                BoardRole.Admin);

            var secondMembership = new BoardMember(
                boardId,
                secondUser.Id,
                BoardRole.Member);

            AttachUser(
                firstMembership,
                firstUser);

            AttachUser(
                secondMembership,
                secondUser);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardMemberAsync(
                    boardId,
                    requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository => repository.GetMembersAsync(
                    boardId))
                .ReturnsAsync(
                    new[]
                    {
                        firstMembership,
                        secondMembership
                    });

            var result = (
                await _service.GetBoardMembersAsync(
                    boardId,
                    requesterId))
                .ToList();

            Assert.Equal(2, result.Count);

            Assert.Equal(
                firstUser.Id,
                result[0].UserId);

            Assert.Equal(
                "alice",
                result[0].Username);

            Assert.Equal(
                BoardRole.Admin,
                result[0].Role);

            Assert.Equal(
                "https://example.com/alice.jpg",
                result[0].ProfileImageUrl);

            Assert.Equal(
                secondUser.Id,
                result[1].UserId);

            Assert.Equal(
                "bob",
                result[1].Username);

            Assert.Equal(
                BoardRole.Member,
                result[1].Role);

            Assert.Null(
                result[1].ProfileImageUrl);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardMemberAsync(
                    boardId,
                    requesterId),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.GetMembersAsync(
                    boardId),
                Times.Once);
        }

        // ============================================================
        // TEST HELPERS
        // ============================================================

        /// <summary>
        /// Associates a User with a BoardMember for tests that simulate
        /// Entity Framework navigation-property loading.
        /// </summary>
        private static void AttachUser(
            BoardMember membership,
            User user)
        {
            var property = typeof(BoardMember)
                .GetProperty(nameof(BoardMember.User));

            Assert.NotNull(property);

            property!.SetValue(
                membership,
                user);
        }
    }
}