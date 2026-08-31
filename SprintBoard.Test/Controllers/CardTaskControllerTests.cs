using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SprintBoard.api.Controllers;
using SprintBoard.api.Services;
using SprintBoard.Application.DTOs.CardTask;
using SprintBoard.Application.Exceptions;
using SprintBoard.Application.Interfaces;
using SprintBoard.Application.Services;
using SprintBoard.Domain.Entities;
using Xunit;

namespace SprintBoard.Test.Controllers
{
    /// <summary>
    /// Contains tests for the <see cref="CardTasksController"/>.
    /// </summary>
    public class CardTasksControllerTests
    {
        private readonly Mock<ICardTaskRepository> _cardTaskRepositoryMock;
        private readonly Mock<ICardRepository> _cardRepositoryMock;
        private readonly Mock<IMembershipAuthorizationService> _membershipAuthorizationServiceMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;

        private readonly CardTaskService _cardTaskService;
        private readonly CardTasksController _controller;

        /// <summary>
        /// Initializes the mocked dependencies and controller instance
        /// used by the card task controller tests.
        /// </summary>
        public CardTasksControllerTests()
        {
            _cardTaskRepositoryMock =
                new Mock<ICardTaskRepository>();

            _cardRepositoryMock =
                new Mock<ICardRepository>();

            _membershipAuthorizationServiceMock =
                new Mock<IMembershipAuthorizationService>();

            _currentUserServiceMock =
                new Mock<ICurrentUserService>();

            _cardTaskService = new CardTaskService(
                _cardTaskRepositoryMock.Object,
                _cardRepositoryMock.Object,
                _membershipAuthorizationServiceMock.Object);

            _controller = new CardTasksController(
                _cardTaskService,
                _currentUserServiceMock.Object);
        }

        // ============================================================
        // CREATE
        // ============================================================

        /// <summary>
        /// Verifies that Create returns HTTP 201 containing
        /// the newly created checklist task.
        /// </summary>
        [Fact]
        public async Task Create_ShouldReturnCreatedAtAction_WhenRequestIsValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var boardId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Test card");

            var request = new CreateCardTaskRequest
            {
                Title = "Create tests",
                Position = 2
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
            var result = await _controller.Create(
                card.Id,
                request);

            // Assert
            var createdResult =
                Assert.IsType<CreatedAtActionResult>(
                    result.Result);

            Assert.Equal(
                StatusCodes.Status201Created,
                createdResult.StatusCode);

            var response =
                Assert.IsType<CardTaskResponse>(
                    createdResult.Value);

            Assert.Equal(
                card.Id,
                response.CardId);

            Assert.Equal(
                "Create tests",
                response.Title);

            Assert.Equal(
                2,
                response.Position);

            Assert.False(
                response.IsCompleted);
        }

        /// <summary>
        /// Verifies that Create configures the created response
        /// to reference the GetByCard action and parent card.
        /// </summary>
        [Fact]
        public async Task Create_ShouldReferenceGetByCardAction()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var boardId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Test card");

            var request = new CreateCardTaskRequest
            {
                Title = "Checklist item"
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
            var result = await _controller.Create(
                card.Id,
                request);

            // Assert
            var createdResult =
                Assert.IsType<CreatedAtActionResult>(
                    result.Result);

            Assert.Equal(
                nameof(CardTasksController.GetByCard),
                createdResult.ActionName);

            Assert.NotNull(
                createdResult.RouteValues);

            Assert.Equal(
                card.Id,
                createdResult.RouteValues!["cardId"]);
        }

        /// <summary>
        /// Verifies that Create uses the identifier of the currently
        /// authenticated user when creating a checklist item.
        /// </summary>
        [Fact]
        public async Task Create_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var card = new Card(
                Guid.NewGuid(),
                "Test card");

            var request = new CreateCardTaskRequest
            {
                Title = "Checklist item"
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
            await _controller.Create(
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
        /// Verifies that Create propagates an UnauthorizedAccessException
        /// when the current user cannot be resolved.
        /// </summary>
        [Fact]
        public async Task Create_ShouldPropagateUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var request = new CreateCardTaskRequest
            {
                Title = "Checklist item"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Throws(
                    new UnauthorizedAccessException(
                        "User is not authenticated."));

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => _controller.Create(
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

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<CardTask>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Create propagates a KeyNotFoundException
        /// when the parent card does not exist.
        /// </summary>
        [Fact]
        public async Task Create_ShouldPropagateKeyNotFoundException_WhenCardDoesNotExist()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var request = new CreateCardTaskRequest
            {
                Title = "Checklist item"
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
                    () => _controller.Create(
                        cardId,
                        request));

            // Assert
            Assert.Equal(
                "Card not found.",
                exception.Message);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<CardTask>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Create propagates a ForbiddenAccessException
        /// and does not persist a checklist item when access is denied.
        /// </summary>
        [Fact]
        public async Task Create_ShouldPropagateForbiddenAccessException_WhenAccessIsDenied()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var card = new Card(
                Guid.NewGuid(),
                "Protected card");

            var request = new CreateCardTaskRequest
            {
                Title = "Unauthorized task"
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
                () => _controller.Create(
                    card.Id,
                    request));

            // Assert
            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<CardTask>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // GET BY CARD
        // ============================================================

        /// <summary>
        /// Verifies that GetByCard returns HTTP 200 containing
        /// all checklist tasks associated with the requested card.
        /// </summary>
        [Fact]
        public async Task GetByCard_ShouldReturnOkWithCardTasks()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var card = new Card(
                Guid.NewGuid(),
                "Test card");

            var firstTask = new CardTask(
                card.Id,
                "First task",
                0);

            var secondTask = new CardTask(
                card.Id,
                "Second task",
                1);

            secondTask.MarkAsCompleted();

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

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByCardAsync(card.Id))
                .ReturnsAsync(
                    new[]
                    {
                        firstTask,
                        secondTask
                    });

            // Act
            var result =
                await _controller.GetByCard(card.Id);

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(
                    result.Result);

            Assert.Equal(
                StatusCodes.Status200OK,
                okResult.StatusCode);

            var tasks =
                Assert.IsAssignableFrom<
                    IEnumerable<CardTaskResponse>>(
                    okResult.Value);

            var list = tasks.ToList();

            Assert.Equal(
                2,
                list.Count);

            Assert.Equal(
                "First task",
                list[0].Title);

            Assert.False(
                list[0].IsCompleted);

            Assert.Equal(
                "Second task",
                list[1].Title);

            Assert.True(
                list[1].IsCompleted);
        }

        /// <summary>
        /// Verifies that GetByCard returns an empty collection
        /// when the card contains no checklist items.
        /// </summary>
        [Fact]
        public async Task GetByCard_ShouldReturnEmptyCollection_WhenCardHasNoTasks()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var card = new Card(
                Guid.NewGuid(),
                "Empty card");

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

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByCardAsync(card.Id))
                .ReturnsAsync(
                    Array.Empty<CardTask>());

            // Act
            var result =
                await _controller.GetByCard(card.Id);

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(
                    result.Result);

            var tasks =
                Assert.IsAssignableFrom<
                    IEnumerable<CardTaskResponse>>(
                    okResult.Value);

            Assert.Empty(tasks);
        }

        /// <summary>
        /// Verifies that GetByCard uses the identifier of the
        /// currently authenticated user.
        /// </summary>
        [Fact]
        public async Task GetByCard_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var card = new Card(
                Guid.NewGuid(),
                "Test card");

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

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByCardAsync(card.Id))
                .ReturnsAsync(
                    Array.Empty<CardTask>());

            // Act
            await _controller.GetByCard(card.Id);

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
        /// Verifies that GetByCard propagates a KeyNotFoundException
        /// when the requested parent card does not exist.
        /// </summary>
        [Fact]
        public async Task GetByCard_ShouldPropagateKeyNotFoundException_WhenCardDoesNotExist()
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
                    () =>
                        _controller.GetByCard(cardId));

            // Assert
            Assert.Equal(
                "Card not found.",
                exception.Message);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.GetByCardAsync(
                        It.IsAny<Guid>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that GetByCard propagates a ForbiddenAccessException
        /// and does not retrieve checklist data when access is denied.
        /// </summary>
        [Fact]
        public async Task GetByCard_ShouldPropagateForbiddenAccessException_WhenAccessIsDenied()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var card = new Card(
                Guid.NewGuid(),
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
                () => _controller.GetByCard(card.Id));

            // Assert
            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.GetByCardAsync(
                        It.IsAny<Guid>()),
                Times.Never);
        }

        // ============================================================
        // MARK AS COMPLETED
        // ============================================================

        /// <summary>
        /// Verifies that MarkAsCompleted returns HTTP 204 and
        /// marks the requested checklist task as completed.
        /// </summary>
        [Fact]
        public async Task MarkAsCompleted_ShouldReturnNoContent_WhenTaskExists()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var task = new CardTask(
                Guid.NewGuid(),
                "Task");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(task.Id))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            var result =
                await _controller.MarkAsCompleted(
                    task.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);

            Assert.True(
                task.IsCompleted);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that MarkAsCompleted uses the identifier
        /// of the currently authenticated user.
        /// </summary>
        [Fact]
        public async Task MarkAsCompleted_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var task = new CardTask(
                Guid.NewGuid(),
                "Task");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(task.Id))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.MarkAsCompleted(
                task.Id);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that MarkAsCompleted propagates a KeyNotFoundException
        /// when the checklist task does not exist.
        /// </summary>
        [Fact]
        public async Task MarkAsCompleted_ShouldPropagateKeyNotFoundException_WhenTaskDoesNotExist()
        {
            // Arrange
            var taskId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(Guid.NewGuid());

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(taskId))
                .ReturnsAsync((CardTask?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () =>
                        _controller.MarkAsCompleted(
                            taskId));

            // Assert
            Assert.Equal(
                "Task not found.",
                exception.Message);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that MarkAsCompleted propagates a ForbiddenAccessException
        /// and leaves the checklist task pending when access is denied.
        /// </summary>
        [Fact]
        public async Task MarkAsCompleted_ShouldPropagateForbiddenAccessException_WhenAccessIsDenied()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var task = new CardTask(
                Guid.NewGuid(),
                "Protected task");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(task.Id))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId))
                .ThrowsAsync(
                    new ForbiddenAccessException(
                        "Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () =>
                    _controller.MarkAsCompleted(
                        task.Id));

            // Assert
            Assert.False(
                task.IsCompleted);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // MARK AS PENDING
        // ============================================================

        /// <summary>
        /// Verifies that MarkAsPending returns HTTP 204 and
        /// marks a completed checklist task as pending.
        /// </summary>
        [Fact]
        public async Task MarkAsPending_ShouldReturnNoContent_WhenTaskExists()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var task = new CardTask(
                Guid.NewGuid(),
                "Completed task");

            task.MarkAsCompleted();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(task.Id))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            var result =
                await _controller.MarkAsPending(
                    task.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);

            Assert.False(
                task.IsCompleted);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that MarkAsPending uses the identifier
        /// of the currently authenticated user.
        /// </summary>
        [Fact]
        public async Task MarkAsPending_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var task = new CardTask(
                Guid.NewGuid(),
                "Completed task");

            task.MarkAsCompleted();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(task.Id))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.MarkAsPending(
                task.Id);

            // Assert
            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that MarkAsPending propagates a KeyNotFoundException
        /// when the checklist task does not exist.
        /// </summary>
        [Fact]
        public async Task MarkAsPending_ShouldPropagateKeyNotFoundException_WhenTaskDoesNotExist()
        {
            // Arrange
            var taskId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(Guid.NewGuid());

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(taskId))
                .ReturnsAsync((CardTask?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () =>
                        _controller.MarkAsPending(
                            taskId));

            // Assert
            Assert.Equal(
                "Task not found.",
                exception.Message);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that MarkAsPending propagates a ForbiddenAccessException
        /// and leaves the checklist task completed when access is denied.
        /// </summary>
        [Fact]
        public async Task MarkAsPending_ShouldPropagateForbiddenAccessException_WhenAccessIsDenied()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var task = new CardTask(
                Guid.NewGuid(),
                "Protected task");

            task.MarkAsCompleted();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(task.Id))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId))
                .ThrowsAsync(
                    new ForbiddenAccessException(
                        "Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () =>
                    _controller.MarkAsPending(
                        task.Id));

            // Assert
            Assert.True(
                task.IsCompleted);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // DELETE
        // ============================================================

        /// <summary>
        /// Verifies that Delete returns HTTP 204 and removes
        /// an existing checklist task.
        /// </summary>
        [Fact]
        public async Task Delete_ShouldReturnNoContent_WhenTaskExists()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var task = new CardTask(
                Guid.NewGuid(),
                "Task to remove");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(task.Id))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            var result =
                await _controller.Delete(task.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(task),
                Times.Once);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Delete uses the identifier of the
        /// currently authenticated user.
        /// </summary>
        [Fact]
        public async Task Delete_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var task = new CardTask(
                Guid.NewGuid(),
                "Task");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(task.Id))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Delete(task.Id);

            // Assert
            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Delete propagates a KeyNotFoundException
        /// when the requested checklist task does not exist.
        /// </summary>
        [Fact]
        public async Task Delete_ShouldPropagateKeyNotFoundException_WhenTaskDoesNotExist()
        {
            // Arrange
            var taskId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(Guid.NewGuid());

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(taskId))
                .ReturnsAsync((CardTask?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () =>
                        _controller.Delete(taskId));

            // Assert
            Assert.Equal(
                "Task not found.",
                exception.Message);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(
                        It.IsAny<CardTask>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Delete propagates a ForbiddenAccessException
        /// and does not remove the checklist task when access is denied.
        /// </summary>
        [Fact]
        public async Task Delete_ShouldPropagateForbiddenAccessException_WhenAccessIsDenied()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var task = new CardTask(
                Guid.NewGuid(),
                "Protected task");

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(task.Id))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId))
                .ThrowsAsync(
                    new ForbiddenAccessException(
                        "Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _controller.Delete(task.Id));

            // Assert
            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(
                        It.IsAny<CardTask>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // UPDATE
        // ============================================================

        /// <summary>
        /// Verifies that Update returns HTTP 204 and updates
        /// the checklist task title when the request is valid.
        /// </summary>
        [Fact]
        public async Task Update_ShouldReturnNoContent_WhenRequestIsValid()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var task = new CardTask(
                Guid.NewGuid(),
                "Original title");

            var request = new UpdateCardTaskRequest
            {
                Title = "   Updated title   "
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(task.Id))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(
                task.Id,
                request);

            // Assert
            Assert.IsType<NoContentResult>(result);

            Assert.Equal(
                "Updated title",
                task.Title);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Update keeps the current title when
        /// a blank title is supplied by the request.
        /// </summary>
        [Fact]
        public async Task Update_ShouldKeepCurrentTitle_WhenTitleIsBlank()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var task = new CardTask(
                Guid.NewGuid(),
                "Original title");

            var request = new UpdateCardTaskRequest
            {
                Title = "   "
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(task.Id))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(
                task.Id,
                request);

            // Assert
            Assert.IsType<NoContentResult>(result);

            Assert.Equal(
                "Original title",
                task.Title);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Update uses the identifier of the
        /// currently authenticated user.
        /// </summary>
        [Fact]
        public async Task Update_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var task = new CardTask(
                Guid.NewGuid(),
                "Original title");

            var request = new UpdateCardTaskRequest
            {
                Title = "Updated title"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(task.Id))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Update(
                task.Id,
                request);

            // Assert
            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Update propagates a KeyNotFoundException
        /// when the requested checklist task does not exist.
        /// </summary>
        [Fact]
        public async Task Update_ShouldPropagateKeyNotFoundException_WhenTaskDoesNotExist()
        {
            // Arrange
            var taskId = Guid.NewGuid();

            var request = new UpdateCardTaskRequest
            {
                Title = "Updated title"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(Guid.NewGuid());

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(taskId))
                .ReturnsAsync((CardTask?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () =>
                        _controller.Update(
                            taskId,
                            request));

            // Assert
            Assert.Equal(
                "Task not found.",
                exception.Message);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Update propagates a ForbiddenAccessException
        /// and leaves the checklist task unchanged when access is denied.
        /// </summary>
        [Fact]
        public async Task Update_ShouldPropagateForbiddenAccessException_WhenAccessIsDenied()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var task = new CardTask(
                Guid.NewGuid(),
                "Original title");

            var request = new UpdateCardTaskRequest
            {
                Title = "Unauthorized title"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _cardTaskRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(task.Id))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureCardTaskBoardMemberAsync(
                        task.Id,
                        userId))
                .ThrowsAsync(
                    new ForbiddenAccessException(
                        "Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () =>
                    _controller.Update(
                        task.Id,
                        request));

            // Assert
            Assert.Equal(
                "Original title",
                task.Title);

            _cardTaskRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }
    }
}