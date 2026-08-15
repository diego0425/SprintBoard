using Moq;
using SprintBoard.Application.Exceptions;
using SprintBoard.Application.Interfaces;
using SprintBoard.Application.Services;
using Xunit;

namespace SprintBoard.Test.Authorization
{
    /// <summary>
    /// Contains unit tests for the <see cref="MembershipAuthorizationService"/>.
    /// </summary>
    public class MembershipAuthorizationServiceTests
    {
        private readonly Mock<IBoardMemberRepository> _boardMemberRepositoryMock;
        private readonly Mock<ICardRepository> _cardRepositoryMock;
        private readonly Mock<ICardTaskRepository> _cardTaskRepositoryMock;
        private readonly MembershipAuthorizationService _service;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="MembershipAuthorizationServiceTests"/> class
        /// and configures the mocked dependencies used by the service.
        /// </summary>
        public MembershipAuthorizationServiceTests()
        {
            _boardMemberRepositoryMock = new Mock<IBoardMemberRepository>();
            _cardRepositoryMock = new Mock<ICardRepository>();
            _cardTaskRepositoryMock = new Mock<ICardTaskRepository>();

            _service = new MembershipAuthorizationService(
                _boardMemberRepositoryMock.Object,
                _cardRepositoryMock.Object,
                _cardTaskRepositoryMock.Object);
        }

        /// <summary>
        /// Verifies that <see cref="MembershipAuthorizationService.EnsureBoardMemberAsync"/>
        /// completes successfully when the user belongs to the specified board.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task EnsureBoardMemberAsync_ShouldComplete_WhenUserIsMember()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _boardMemberRepositoryMock
                .Setup(repository => repository.ExistsAsync(boardId, userId))
                .ReturnsAsync(true);

            // Act
            await _service.EnsureBoardMemberAsync(boardId, userId);

            // Assert
            _boardMemberRepositoryMock.Verify(
                repository => repository.ExistsAsync(boardId, userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="MembershipAuthorizationService.EnsureBoardMemberAsync"/>
        /// throws a <see cref="ForbiddenAccessException"/> when the user does not
        /// belong to the specified board.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task EnsureBoardMemberAsync_ShouldThrowForbiddenAccessException_WhenUserIsNotMember()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _boardMemberRepositoryMock
                .Setup(repository => repository.ExistsAsync(boardId, userId))
                .ReturnsAsync(false);

            // Act
            var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.EnsureBoardMemberAsync(boardId, userId));

            // Assert
            Assert.Equal(
                "You are not a member of this board.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository => repository.ExistsAsync(boardId, userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="MembershipAuthorizationService.EnsureBoardOwnerAsync"/>
        /// completes successfully when the user is the owner of the specified board.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task EnsureBoardOwnerAsync_ShouldComplete_WhenUserIsOwner()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _boardMemberRepositoryMock
                .Setup(repository => repository.IsOwnerAsync(boardId, userId))
                .ReturnsAsync(true);

            // Act
            await _service.EnsureBoardOwnerAsync(boardId, userId);

            // Assert
            _boardMemberRepositoryMock.Verify(
                repository => repository.IsOwnerAsync(boardId, userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="MembershipAuthorizationService.EnsureBoardOwnerAsync"/>
        /// throws a <see cref="ForbiddenAccessException"/> when the user is not
        /// the owner of the specified board.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task EnsureBoardOwnerAsync_ShouldThrowForbiddenAccessException_WhenUserIsNotOwner()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _boardMemberRepositoryMock
                .Setup(repository => repository.IsOwnerAsync(boardId, userId))
                .ReturnsAsync(false);

            // Act
            var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.EnsureBoardOwnerAsync(boardId, userId));

            // Assert
            Assert.Equal(
                "Only the board owner can perform this action.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository => repository.IsOwnerAsync(boardId, userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="MembershipAuthorizationService.EnsureBoardOwnerOrAdminAsync"/>
        /// completes successfully when the user has owner or administrator privileges
        /// on the specified board.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task EnsureBoardOwnerOrAdminAsync_ShouldComplete_WhenUserIsOwnerOrAdmin()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _boardMemberRepositoryMock
                .Setup(repository => repository.IsOwnerOrAdminAsync(boardId, userId))
                .ReturnsAsync(true);

            // Act
            await _service.EnsureBoardOwnerOrAdminAsync(boardId, userId);

            // Assert
            _boardMemberRepositoryMock.Verify(
                repository => repository.IsOwnerOrAdminAsync(boardId, userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="MembershipAuthorizationService.EnsureBoardOwnerOrAdminAsync"/>
        /// throws a <see cref="ForbiddenAccessException"/> when the user has neither
        /// owner nor administrator privileges on the specified board.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task EnsureBoardOwnerOrAdminAsync_ShouldThrowForbiddenAccessException_WhenUserHasNoPermission()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _boardMemberRepositoryMock
                .Setup(repository => repository.IsOwnerOrAdminAsync(boardId, userId))
                .ReturnsAsync(false);

            // Act
            var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.EnsureBoardOwnerOrAdminAsync(boardId, userId));

            // Assert
            Assert.Equal(
                "You do not have permission to perform this action.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository => repository.IsOwnerOrAdminAsync(boardId, userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="MembershipAuthorizationService.EnsureCardBoardMemberAsync"/>
        /// completes successfully when the card exists and the user belongs
        /// to the board that contains the card.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task EnsureCardBoardMemberAsync_ShouldComplete_WhenCardExistsAndUserIsBoardMember()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _cardRepositoryMock
                .Setup(repository => repository.GetBoardIdByCardIdAsync(cardId))
                .ReturnsAsync(boardId);

            _boardMemberRepositoryMock
                .Setup(repository => repository.ExistsAsync(boardId, userId))
                .ReturnsAsync(true);

            // Act
            await _service.EnsureCardBoardMemberAsync(cardId, userId);

            // Assert
            _cardRepositoryMock.Verify(
                repository => repository.GetBoardIdByCardIdAsync(cardId),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.ExistsAsync(boardId, userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="MembershipAuthorizationService.EnsureCardBoardMemberAsync"/>
        /// throws a <see cref="KeyNotFoundException"/> when the specified card
        /// does not exist.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task EnsureCardBoardMemberAsync_ShouldThrowKeyNotFoundException_WhenCardDoesNotExist()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _cardRepositoryMock
                .Setup(repository => repository.GetBoardIdByCardIdAsync(cardId))
                .ReturnsAsync((Guid?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.EnsureCardBoardMemberAsync(cardId, userId));

            // Assert
            Assert.Equal(
                "Card not found.",
                exception.Message);

            _cardRepositoryMock.Verify(
                repository => repository.GetBoardIdByCardIdAsync(cardId),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.ExistsAsync(It.IsAny<Guid>(), userId),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="MembershipAuthorizationService.EnsureCardBoardMemberAsync"/>
        /// throws a <see cref="ForbiddenAccessException"/> when the card exists
        /// but the user does not belong to its board.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task EnsureCardBoardMemberAsync_ShouldThrowForbiddenAccessException_WhenUserIsNotBoardMember()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _cardRepositoryMock
                .Setup(repository => repository.GetBoardIdByCardIdAsync(cardId))
                .ReturnsAsync(boardId);

            _boardMemberRepositoryMock
                .Setup(repository => repository.ExistsAsync(boardId, userId))
                .ReturnsAsync(false);

            // Act
            var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.EnsureCardBoardMemberAsync(cardId, userId));

            // Assert
            Assert.Equal(
                "You are not a member of this board.",
                exception.Message);

            _cardRepositoryMock.Verify(
                repository => repository.GetBoardIdByCardIdAsync(cardId),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.ExistsAsync(boardId, userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="MembershipAuthorizationService.EnsureCardTaskBoardMemberAsync"/>
        /// completes successfully when the checklist task and its parent card exist
        /// and the user belongs to the board that contains them.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task EnsureCardTaskBoardMemberAsync_ShouldComplete_WhenTaskCardAndMembershipExist()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var cardId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetCardIdByTaskIdAsync(taskId))
                .ReturnsAsync(cardId);

            _cardRepositoryMock
                .Setup(repository => repository.GetBoardIdByCardIdAsync(cardId))
                .ReturnsAsync(boardId);

            _boardMemberRepositoryMock
                .Setup(repository => repository.ExistsAsync(boardId, userId))
                .ReturnsAsync(true);

            // Act
            await _service.EnsureCardTaskBoardMemberAsync(taskId, userId);

            // Assert
            _cardTaskRepositoryMock.Verify(
                repository => repository.GetCardIdByTaskIdAsync(taskId),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository => repository.GetBoardIdByCardIdAsync(cardId),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.ExistsAsync(boardId, userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="MembershipAuthorizationService.EnsureCardTaskBoardMemberAsync"/>
        /// throws a <see cref="KeyNotFoundException"/> when the specified checklist
        /// task does not exist.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task EnsureCardTaskBoardMemberAsync_ShouldThrowKeyNotFoundException_WhenTaskDoesNotExist()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetCardIdByTaskIdAsync(taskId))
                .ReturnsAsync((Guid?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.EnsureCardTaskBoardMemberAsync(taskId, userId));

            // Assert
            Assert.Equal(
                "Task not found.",
                exception.Message);

            _cardTaskRepositoryMock.Verify(
                repository => repository.GetCardIdByTaskIdAsync(taskId),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository => repository.GetBoardIdByCardIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="MembershipAuthorizationService.EnsureCardTaskBoardMemberAsync"/>
        /// throws a <see cref="KeyNotFoundException"/> when the checklist task exists
        /// but its parent card does not exist.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task EnsureCardTaskBoardMemberAsync_ShouldThrowKeyNotFoundException_WhenParentCardDoesNotExist()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetCardIdByTaskIdAsync(taskId))
                .ReturnsAsync(cardId);

            _cardRepositoryMock
                .Setup(repository => repository.GetBoardIdByCardIdAsync(cardId))
                .ReturnsAsync((Guid?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.EnsureCardTaskBoardMemberAsync(taskId, userId));

            // Assert
            Assert.Equal(
                "Card not found.",
                exception.Message);

            _cardTaskRepositoryMock.Verify(
                repository => repository.GetCardIdByTaskIdAsync(taskId),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository => repository.GetBoardIdByCardIdAsync(cardId),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.ExistsAsync(It.IsAny<Guid>(), userId),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="MembershipAuthorizationService.EnsureCardTaskBoardMemberAsync"/>
        /// throws a <see cref="ForbiddenAccessException"/> when the checklist task
        /// and its parent card exist but the user does not belong to the board.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task EnsureCardTaskBoardMemberAsync_ShouldThrowForbiddenAccessException_WhenUserIsNotBoardMember()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var cardId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetCardIdByTaskIdAsync(taskId))
                .ReturnsAsync(cardId);

            _cardRepositoryMock
                .Setup(repository => repository.GetBoardIdByCardIdAsync(cardId))
                .ReturnsAsync(boardId);

            _boardMemberRepositoryMock
                .Setup(repository => repository.ExistsAsync(boardId, userId))
                .ReturnsAsync(false);

            // Act
            var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.EnsureCardTaskBoardMemberAsync(taskId, userId));

            // Assert
            Assert.Equal(
                "You are not a member of this board.",
                exception.Message);

            _cardTaskRepositoryMock.Verify(
                repository => repository.GetCardIdByTaskIdAsync(taskId),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository => repository.GetBoardIdByCardIdAsync(cardId),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.ExistsAsync(boardId, userId),
                Times.Once);
        }
    }
}