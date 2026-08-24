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
    }
}