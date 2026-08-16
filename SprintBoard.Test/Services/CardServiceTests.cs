using Moq;
using SprintBoard.Application.DTOs.Card;
using SprintBoard.Application.Interfaces;
using SprintBoard.Application.Services;
using SprintBoard.Domain.Entities;
using SprintBoard.Domain.Enums;
using Xunit;

namespace SprintBoard.Test.Services
{
    /// <summary>
    /// Contains unit tests for the <see cref="CardService"/>.
    /// </summary>
    public class CardServiceTests
    {
        private readonly Mock<IBoardRepository> _boardRepositoryMock;
        private readonly Mock<ICardRepository> _cardRepositoryMock;
        private readonly Mock<IMembershipAuthorizationService> _membershipAuthorizationServiceMock;
        private readonly CardService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="CardServiceTests"/> class
        /// and configures the mocked dependencies used by the service.
        /// </summary>
        public CardServiceTests()
        {
            _boardRepositoryMock = new Mock<IBoardRepository>();
            _cardRepositoryMock = new Mock<ICardRepository>();
            _membershipAuthorizationServiceMock =
                new Mock<IMembershipAuthorizationService>();

            _service = new CardService(
                _boardRepositoryMock.Object,
                _cardRepositoryMock.Object,
                _membershipAuthorizationServiceMock.Object);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.CreateAsync"/> throws an
        /// <see cref="ArgumentException"/> when the board identifier is empty.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentException_WhenBoardIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var request = new CreateCardRequest
            {
                Title = "New card"
            };

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(Guid.Empty, userId, request));

            // Assert
            Assert.Equal("BoardId cannot be empty.", exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.CreateAsync"/> throws an
        /// <see cref="ArgumentException"/> when the card title is empty.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentException_WhenTitleIsEmpty()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var request = new CreateCardRequest
            {
                Title = "   "
            };

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(boardId, userId, request));

            // Assert
            Assert.Equal("Title cannot be empty.", exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(boardId),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.CreateAsync"/> throws a
        /// <see cref="KeyNotFoundException"/> when the specified board
        /// does not exist.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task CreateAsync_ShouldThrowKeyNotFoundException_WhenBoardDoesNotExist()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var request = new CreateCardRequest
            {
                Title = "New card"
            };

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.CreateAsync(boardId, userId, request));

            // Assert
            Assert.Equal("Board not found.", exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardMemberAsync(boardId, userId),
                Times.Never);

            _cardRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<Card>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.CreateAsync"/> creates and
        /// persists a card with position zero when no position is supplied.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task CreateAsync_ShouldCreateCard_WithDefaultPosition()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board = new Board("Test Board", userId);

            var request = new CreateCardRequest
            {
                Title = "New card",
                Description = "Card description"
            };

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardMemberAsync(boardId, userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(boardId, userId, request);

            // Assert
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(boardId, result.BoardId);
            Assert.Equal("New card", result.Title);
            Assert.Equal("Card description", result.Description);
            Assert.Equal(0, result.Position);
            Assert.Equal(CardStatus.ToDo, result.Status);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardMemberAsync(boardId, userId),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<Card>(card =>
                        card.BoardId == boardId &&
                        card.Title == "New card" &&
                        card.Position == 0)),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.CreateAsync"/> preserves the
        /// requested card position when a position is explicitly provided.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task CreateAsync_ShouldCreateCard_WithProvidedPosition()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board = new Board("Test Board", userId);

            var request = new CreateCardRequest
            {
                Title = "Positioned card",
                Position = 5
            };

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardMemberAsync(boardId, userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(boardId, userId, request);

            // Assert
            Assert.Equal(5, result.Position);

            _cardRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<Card>(card => card.Position == 5)),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.GetByBoardAsync"/> throws an
        /// <see cref="ArgumentException"/> when the board identifier is empty.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task GetByBoardAsync_ShouldThrowArgumentException_WhenBoardIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetByBoardAsync(Guid.Empty, userId));

            // Assert
            Assert.Equal("BoardId cannot be empty.", exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.GetByBoardAsync"/> throws a
        /// <see cref="KeyNotFoundException"/> when the board does not exist.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task GetByBoardAsync_ShouldThrowKeyNotFoundException_WhenBoardDoesNotExist()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.GetByBoardAsync(boardId, userId));

            // Assert
            Assert.Equal("Board not found.", exception.Message);

            _cardRepositoryMock.Verify(
                repository => repository.GetByBoardAsync(boardId),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.GetByBoardAsync"/> returns
        /// cards ordered by workflow status in descending order.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task GetByBoardAsync_ShouldReturnCards_OrderedByStatusDescending()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board = new Board("Test Board", userId);

            var todoCard = new Card(boardId, "To Do", null, 0);

            var doingCard = new Card(boardId, "Doing", null, 1);
            doingCard.ChangeStatus(CardStatus.Doing);

            var doneCard = new Card(boardId, "Done", null, 2);
            doneCard.ChangeStatus(CardStatus.Done);

            var cards = new List<Card>
            {
                todoCard,
                doneCard,
                doingCard
            };

            _boardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureBoardMemberAsync(boardId, userId))
                .Returns(Task.CompletedTask);

            _cardRepositoryMock
                .Setup(repository => repository.GetByBoardAsync(boardId))
                .ReturnsAsync(cards);

            // Act
            var result = (await _service.GetByBoardAsync(boardId, userId)).ToList();

            // Assert
            Assert.Equal(3, result.Count);

            Assert.Equal(CardStatus.Done, result[0].Status);
            Assert.Equal(CardStatus.Doing, result[1].Status);
            Assert.Equal(CardStatus.ToDo, result[2].Status);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureBoardMemberAsync(boardId, userId),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository => repository.GetByBoardAsync(boardId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.ChangeStatusAsync"/> throws an
        /// <see cref="ArgumentException"/> when the card identifier is empty.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task ChangeStatusAsync_ShouldThrowArgumentException_WhenCardIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ChangeStatusAsync(
                    Guid.Empty,
                    userId,
                    CardStatus.Doing));

            // Assert
            Assert.Equal("CardId cannot be empty.", exception.Message);

            _cardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.ChangeStatusAsync"/> throws a
        /// <see cref="KeyNotFoundException"/> when the card does not exist.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task ChangeStatusAsync_ShouldThrowKeyNotFoundException_WhenCardDoesNotExist()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync((Card?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.ChangeStatusAsync(
                    cardId,
                    userId,
                    CardStatus.Doing));

            // Assert
            Assert.Equal("Card not found.", exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardBoardMemberAsync(cardId, userId),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.ChangeStatusAsync"/> updates
        /// the card workflow status and persists the change.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task ChangeStatusAsync_ShouldChangeStatus_WhenCardExists()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var card = new Card(boardId, "Test Card");

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardBoardMemberAsync(cardId, userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.ChangeStatusAsync(
                cardId,
                userId,
                CardStatus.Doing);

            // Assert
            Assert.Equal(CardStatus.Doing, card.Status);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardBoardMemberAsync(cardId, userId),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.ChangeStatusAsync"/> throws an
        /// <see cref="ArgumentException"/> when an invalid workflow status
        /// is supplied.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task ChangeStatusAsync_ShouldThrowArgumentException_WhenStatusIsInvalid()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var card = new Card(boardId, "Test Card");

            var invalidStatus = (CardStatus)999;

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardBoardMemberAsync(cardId, userId))
                .Returns(Task.CompletedTask);

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ChangeStatusAsync(
                    cardId,
                    userId,
                    invalidStatus));

            // Assert
            Assert.Contains("Card status is invalid.", exception.Message);

            _cardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.RemoveAsync"/> throws an
        /// <see cref="ArgumentException"/> when the card identifier is empty.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task RemoveAsync_ShouldThrowArgumentException_WhenCardIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RemoveAsync(Guid.Empty, userId));

            // Assert
            Assert.Equal("CardId cannot be empty.", exception.Message);

            _cardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.RemoveAsync"/> throws a
        /// <see cref="KeyNotFoundException"/> when the card does not exist.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task RemoveAsync_ShouldThrowKeyNotFoundException_WhenCardDoesNotExist()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync((Card?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.RemoveAsync(cardId, userId));

            // Assert
            Assert.Equal("Card not found.", exception.Message);

            _cardRepositoryMock.Verify(
                repository => repository.RemoveAsync(It.IsAny<Card>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.RemoveAsync"/> removes an
        /// existing card and persists the change.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task RemoveAsync_ShouldRemoveCard_WhenCardExists()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var card = new Card(boardId, "Card to remove");

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardBoardMemberAsync(cardId, userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.RemoveAsync(cardId, userId);

            // Assert
            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardBoardMemberAsync(cardId, userId),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository => repository.RemoveAsync(card),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.UpdateAsync"/> throws an
        /// <see cref="ArgumentException"/> when the card identifier is empty.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task UpdateAsync_ShouldThrowArgumentException_WhenCardIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var request = new UpdateCardRequest
            {
                Title = "Updated title"
            };

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateAsync(
                    Guid.Empty,
                    userId,
                    request));

            // Assert
            Assert.Equal("CardId cannot be empty.", exception.Message);

            _cardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.UpdateAsync"/> throws a
        /// <see cref="KeyNotFoundException"/> when the card does not exist.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task UpdateAsync_ShouldThrowKeyNotFoundException_WhenCardDoesNotExist()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var request = new UpdateCardRequest
            {
                Title = "Updated title"
            };

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync((Card?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateAsync(
                    cardId,
                    userId,
                    request));

            // Assert
            Assert.Equal("Card not found.", exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardBoardMemberAsync(cardId, userId),
                Times.Never);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.UpdateAsync"/> updates both
        /// the title and description of an existing card.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateTitleAndDescription()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Original title",
                "Original description");

            var request = new UpdateCardRequest
            {
                Title = "Updated title",
                Description = "Updated description"
            };

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardBoardMemberAsync(cardId, userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAsync(cardId, userId, request);

            // Assert
            Assert.Equal("Updated title", card.Title);
            Assert.Equal("Updated description", card.Description);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardBoardMemberAsync(cardId, userId),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.UpdateAsync"/> leaves the
        /// existing title unchanged when a blank title is supplied.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task UpdateAsync_ShouldKeepTitle_WhenTitleIsBlank()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Original title",
                "Original description");

            var request = new UpdateCardRequest
            {
                Title = "   "
            };

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardBoardMemberAsync(cardId, userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAsync(cardId, userId, request);

            // Assert
            Assert.Equal("Original title", card.Title);
            Assert.Equal("Original description", card.Description);

            _cardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.UpdateAsync"/> preserves the
        /// current description when no new description is supplied.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task UpdateAsync_ShouldKeepDescription_WhenDescriptionIsNull()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Original title",
                "Original description");

            var request = new UpdateCardRequest
            {
                Title = "Updated title",
                Description = null
            };

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardBoardMemberAsync(cardId, userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAsync(cardId, userId, request);

            // Assert
            Assert.Equal("Updated title", card.Title);
            Assert.Equal("Original description", card.Description);

            _cardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="CardService.UpdateAsync"/> removes the
        /// current description when an empty description is supplied.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous unit test.
        /// </returns>
        [Fact]
        public async Task UpdateAsync_ShouldRemoveDescription_WhenDescriptionIsEmpty()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Original title",
                "Original description");

            var request = new UpdateCardRequest
            {
                Description = ""
            };

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardBoardMemberAsync(cardId, userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAsync(cardId, userId, request);

            // Assert
            Assert.Equal("Original title", card.Title);
            Assert.Null(card.Description);

            _cardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }
    }
}