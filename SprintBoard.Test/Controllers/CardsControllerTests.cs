using Microsoft.AspNetCore.Mvc;
using Moq;
using SprintBoard.api.Controllers;
using SprintBoard.api.Services;
using SprintBoard.Application.DTOs.Card;
using SprintBoard.Application.Exceptions;
using SprintBoard.Application.Interfaces;
using SprintBoard.Application.Services;
using SprintBoard.Domain.Entities;
using SprintBoard.Domain.Enums;
using Xunit;

namespace SprintBoard.Test.Controllers
{
    /// <summary>
    /// Contains tests for the <see cref="CardsController"/>.
    /// </summary>
    public class CardsControllerTests
    {
        private readonly Mock<IBoardRepository> _boardRepositoryMock;
        private readonly Mock<ICardRepository> _cardRepositoryMock;
        private readonly Mock<IMembershipAuthorizationService> _membershipAuthorizationServiceMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;

        private readonly CardService _cardService;
        private readonly CardsController _controller;

        /// <summary>
        /// Initializes the mocked dependencies and controller instance
        /// used by the card controller tests.
        /// </summary>
        public CardsControllerTests()
        {
            _boardRepositoryMock =
                new Mock<IBoardRepository>();

            _cardRepositoryMock =
                new Mock<ICardRepository>();

            _membershipAuthorizationServiceMock =
                new Mock<IMembershipAuthorizationService>();

            _currentUserServiceMock =
                new Mock<ICurrentUserService>();

            _cardService = new CardService(
                _boardRepositoryMock.Object,
                _cardRepositoryMock.Object,
                _membershipAuthorizationServiceMock.Object);

            _controller = new CardsController(
                _cardService,
                _currentUserServiceMock.Object);
        }

        // ============================================================
        // CHANGE STATUS
        // ============================================================

        /// <summary>
        /// Verifies that ChangeStatus returns HTTP 204 and updates
        /// the card workflow status when the request is valid.
        /// </summary>
        [Fact]
        public async Task ChangeStatus_ShouldReturnNoContent_WhenRequestIsValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var boardId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Test card");

            var request = new UpdateCardStatusRequest
            {
                Status = CardStatus.Doing
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(card.Id))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardBoardMemberAsync(
                        card.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ChangeStatus(
                card.Id,
                request);

            // Assert
            Assert.IsType<NoContentResult>(result);

            Assert.Equal(
                CardStatus.Doing,
                card.Status);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that ChangeStatus uses the identifier of the
        /// currently authenticated user.
        /// </summary>
        [Fact]
        public async Task ChangeStatus_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var boardId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Test card");

            var request = new UpdateCardStatusRequest
            {
                Status = CardStatus.Done
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(card.Id))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardBoardMemberAsync(
                        card.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.ChangeStatus(
                card.Id,
                request);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureCardBoardMemberAsync(
                        card.Id,
                        userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that ChangeStatus propagates an
        /// UnauthorizedAccessException when no authenticated user is available.
        /// </summary>
        [Fact]
        public async Task ChangeStatus_ShouldPropagateUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var request = new UpdateCardStatusRequest
            {
                Status = CardStatus.Doing
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Throws(
                    new UnauthorizedAccessException(
                        "User is not authenticated."));

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => _controller.ChangeStatus(
                        Guid.NewGuid(),
                        request));

            // Assert
            Assert.Equal(
                "User is not authenticated.",
                exception.Message);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.GetByIdAsync(
                        It.IsAny<Guid>()),
                Times.Never);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that ChangeStatus propagates a KeyNotFoundException
        /// when the requested card does not exist.
        /// </summary>
        [Fact]
        public async Task ChangeStatus_ShouldPropagateKeyNotFoundException_WhenCardDoesNotExist()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var request = new UpdateCardStatusRequest
            {
                Status = CardStatus.Doing
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(cardId))
                .ReturnsAsync((Card?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => _controller.ChangeStatus(
                        cardId,
                        request));

            // Assert
            Assert.Equal(
                "Card not found.",
                exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureCardBoardMemberAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>()),
                Times.Never);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that ChangeStatus propagates an ArgumentException
        /// when an invalid workflow status is supplied.
        /// </summary>
        [Fact]
        public async Task ChangeStatus_ShouldPropagateArgumentException_WhenStatusIsInvalid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var boardId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Test card");

            var request = new UpdateCardStatusRequest
            {
                Status = (CardStatus)999
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(card.Id))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardBoardMemberAsync(
                        card.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            var exception =
                await Assert.ThrowsAsync<ArgumentException>(
                    () => _controller.ChangeStatus(
                        card.Id,
                        request));

            // Assert
            Assert.Contains(
                "Card status is invalid.",
                exception.Message);

            Assert.Equal(
                CardStatus.ToDo,
                card.Status);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // DELETE
        // ============================================================

        /// <summary>
        /// Verifies that Delete returns HTTP 204 and removes
        /// the requested card when the operation is valid.
        /// </summary>
        [Fact]
        public async Task Delete_ShouldReturnNoContent_WhenCardExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var boardId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Card to delete");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(card.Id))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardBoardMemberAsync(
                        card.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            var result =
                await _controller.Delete(card.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(card),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Delete uses the authenticated user's identifier
        /// when checking permission to remove a card.
        /// </summary>
        [Fact]
        public async Task Delete_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var boardId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Card to delete");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(card.Id))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardBoardMemberAsync(
                        card.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Delete(card.Id);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureCardBoardMemberAsync(
                        card.Id,
                        userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Delete propagates an UnauthorizedAccessException
        /// when the current user cannot be resolved.
        /// </summary>
        [Fact]
        public async Task Delete_ShouldPropagateUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Throws(
                    new UnauthorizedAccessException(
                        "User is not authenticated."));

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => _controller.Delete(
                        Guid.NewGuid()));

            // Assert
            Assert.Equal(
                "User is not authenticated.",
                exception.Message);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.GetByIdAsync(
                        It.IsAny<Guid>()),
                Times.Never);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(
                        It.IsAny<Card>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Delete propagates a KeyNotFoundException
        /// when the requested card cannot be found.
        /// </summary>
        [Fact]
        public async Task Delete_ShouldPropagateKeyNotFoundException_WhenCardDoesNotExist()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(cardId))
                .ReturnsAsync((Card?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => _controller.Delete(cardId));

            // Assert
            Assert.Equal(
                "Card not found.",
                exception.Message);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(
                        It.IsAny<Card>()),
                Times.Never);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Delete propagates a ForbiddenAccessException
        /// and does not remove the card when authorization fails.
        /// </summary>
        [Fact]
        public async Task Delete_ShouldPropagateForbiddenAccessException_WhenAccessIsDenied()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var boardId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Protected card");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(card.Id))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardBoardMemberAsync(
                        card.Id,
                        userId))
                .ThrowsAsync(
                    new ForbiddenAccessException(
                        "Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _controller.Delete(card.Id));

            // Assert
            _cardRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(
                        It.IsAny<Card>()),
                Times.Never);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // UPDATE
        // ============================================================

        /// <summary>
        /// Verifies that Update returns HTTP 204 and applies the
        /// requested card changes when the operation is valid.
        /// </summary>
        [Fact]
        public async Task Update_ShouldReturnNoContent_WhenRequestIsValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var boardId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Original title",
                "Original description");

            var request = new UpdateCardRequest
            {
                Title = "   Updated title   ",
                Description = "   Updated description   "
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(card.Id))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardBoardMemberAsync(
                        card.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(
                card.Id,
                request);

            // Assert
            Assert.IsType<NoContentResult>(result);

            Assert.Equal(
                "Updated title",
                card.Title);

            Assert.Equal(
                "Updated description",
                card.Description);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Update uses the identifier of the currently
        /// authenticated user when authorizing card changes.
        /// </summary>
        [Fact]
        public async Task Update_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var boardId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Original title");

            var request = new UpdateCardRequest
            {
                Title = "Updated title"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(card.Id))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardBoardMemberAsync(
                        card.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Update(
                card.Id,
                request);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureCardBoardMemberAsync(
                        card.Id,
                        userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Update propagates an UnauthorizedAccessException
        /// when no authenticated user identifier can be resolved.
        /// </summary>
        [Fact]
        public async Task Update_ShouldPropagateUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var request = new UpdateCardRequest
            {
                Title = "Updated title"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Throws(
                    new UnauthorizedAccessException(
                        "User is not authenticated."));

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => _controller.Update(
                        Guid.NewGuid(),
                        request));

            // Assert
            Assert.Equal(
                "User is not authenticated.",
                exception.Message);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.GetByIdAsync(
                        It.IsAny<Guid>()),
                Times.Never);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Update propagates a KeyNotFoundException
        /// when the requested card does not exist.
        /// </summary>
        [Fact]
        public async Task Update_ShouldPropagateKeyNotFoundException_WhenCardDoesNotExist()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var request = new UpdateCardRequest
            {
                Title = "Updated title"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(cardId))
                .ReturnsAsync((Card?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => _controller.Update(
                        cardId,
                        request));

            // Assert
            Assert.Equal(
                "Card not found.",
                exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureCardBoardMemberAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>()),
                Times.Never);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Update propagates a ForbiddenAccessException
        /// and leaves the card unchanged when authorization fails.
        /// </summary>
        [Fact]
        public async Task Update_ShouldPropagateForbiddenAccessException_WhenAccessIsDenied()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var boardId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Original title",
                "Original description");

            var request = new UpdateCardRequest
            {
                Title = "Unauthorized title",
                Description = "Unauthorized description"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(card.Id))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardBoardMemberAsync(
                        card.Id,
                        userId))
                .ThrowsAsync(
                    new ForbiddenAccessException(
                        "Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _controller.Update(
                    card.Id,
                    request));

            // Assert
            Assert.Equal(
                "Original title",
                card.Title);

            Assert.Equal(
                "Original description",
                card.Description);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }
    }
}