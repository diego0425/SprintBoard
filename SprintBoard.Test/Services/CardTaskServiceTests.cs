using Moq;
using SprintBoard.Application.DTOs.CardTask;
using SprintBoard.Application.Exceptions;
using SprintBoard.Application.Interfaces;
using SprintBoard.Application.Services;
using SprintBoard.Domain.Entities;
using Xunit;

namespace SprintBoard.Test.Services
{
    /// <summary>
    /// Contains unit tests for the <see cref="CardTaskService"/>.
    /// </summary>
    public class CardTaskServiceTests
    {
        private readonly Mock<ICardTaskRepository> _cardTaskRepositoryMock;
        private readonly Mock<ICardRepository> _cardRepositoryMock;
        private readonly Mock<IMembershipAuthorizationService> _membershipAuthorizationServiceMock;
        private readonly CardTaskService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="CardTaskServiceTests"/> class
        /// and configures the mocked dependencies used by the service.
        /// </summary>
        public CardTaskServiceTests()
        {
            _cardTaskRepositoryMock = new Mock<ICardTaskRepository>();
            _cardRepositoryMock = new Mock<ICardRepository>();
            _membershipAuthorizationServiceMock =
                new Mock<IMembershipAuthorizationService>();

            _service = new CardTaskService(
                _cardTaskRepositoryMock.Object,
                _cardRepositoryMock.Object,
                _membershipAuthorizationServiceMock.Object);
        }

        // ============================================================
        // CREATE
        // ============================================================

        /// <summary>
        /// Verifies that CreateAsync throws an ArgumentException
        /// when the card identifier is empty.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentException_WhenCardIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var request = new CreateCardTaskRequest
            {
                Title = "New checklist task"
            };

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(
                    Guid.Empty,
                    userId,
                    request));

            // Assert
            Assert.Equal("CardId cannot be empty.", exception.Message);

            _cardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<CardTask>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that CreateAsync throws an ArgumentException
        /// when the checklist task title is blank.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentException_WhenTitleIsEmpty()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var request = new CreateCardTaskRequest
            {
                Title = "   "
            };

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(
                    cardId,
                    userId,
                    request));

            // Assert
            Assert.Equal("Title cannot be empty.", exception.Message);

            _cardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<CardTask>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that CreateAsync throws a KeyNotFoundException
        /// when the parent card does not exist.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowKeyNotFoundException_WhenCardDoesNotExist()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var request = new CreateCardTaskRequest
            {
                Title = "New checklist task"
            };

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync((Card?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.CreateAsync(
                    cardId,
                    userId,
                    request));

            // Assert
            Assert.Equal("Card not found.", exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardBoardMemberAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<CardTask>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that CreateAsync creates a checklist task
        /// with position zero when no position is supplied.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateCardTask_WithDefaultPosition()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Test card");

            var request = new CreateCardTaskRequest
            {
                Title = "Create automated tests"
            };

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardBoardMemberAsync(
                    cardId,
                    userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(
                cardId,
                userId,
                request);

            // Assert
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(cardId, result.CardId);
            Assert.Equal("Create automated tests", result.Title);
            Assert.False(result.IsCompleted);
            Assert.Equal(0, result.Position);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardBoardMemberAsync(
                    cardId,
                    userId),
                Times.Once);

            _cardTaskRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<CardTask>(task =>
                        task.CardId == cardId &&
                        task.Title == "Create automated tests" &&
                        task.Position == 0 &&
                        !task.IsCompleted)),
                Times.Once);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that CreateAsync preserves an explicitly
        /// supplied checklist task position.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateCardTask_WithProvidedPosition()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Test card");

            var request = new CreateCardTaskRequest
            {
                Title = "Third checklist task",
                Position = 3
            };

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardBoardMemberAsync(
                    cardId,
                    userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(
                cardId,
                userId,
                request);

            // Assert
            Assert.Equal(cardId, result.CardId);
            Assert.Equal("Third checklist task", result.Title);
            Assert.Equal(3, result.Position);
            Assert.False(result.IsCompleted);

            _cardTaskRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<CardTask>(task =>
                        task.CardId == cardId &&
                        task.Position == 3)),
                Times.Once);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that CreateAsync trims whitespace
        /// from the checklist task title.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldTrimTitle()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Test card");

            var request = new CreateCardTaskRequest
            {
                Title = "   Checklist task   "
            };

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardBoardMemberAsync(
                    cardId,
                    userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(
                cardId,
                userId,
                request);

            // Assert
            Assert.Equal("Checklist task", result.Title);

            _cardTaskRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<CardTask>(task =>
                        task.Title == "Checklist task")),
                Times.Once);
        }

        /// <summary>
        /// Verifies that CreateAsync does not persist a checklist
        /// task when the requested position is negative.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowArgumentException_WhenPositionIsNegative()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Test card");

            var request = new CreateCardTaskRequest
            {
                Title = "Invalid task",
                Position = -1
            };

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardBoardMemberAsync(
                    cardId,
                    userId))
                .Returns(Task.CompletedTask);

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(
                    cardId,
                    userId,
                    request));

            // Assert
            Assert.Contains("Position cannot be negative.", exception.Message);

            _cardTaskRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<CardTask>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that CreateAsync does not persist data
        /// when board membership authorization fails.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldNotCreateCardTask_WhenAccessIsForbidden()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Test card");

            var request = new CreateCardTaskRequest
            {
                Title = "Forbidden task"
            };

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardBoardMemberAsync(
                    cardId,
                    userId))
                .ThrowsAsync(
                    new ForbiddenAccessException("Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.CreateAsync(
                    cardId,
                    userId,
                    request));

            // Assert
            _cardTaskRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<CardTask>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // GET BY CARD
        // ============================================================

        /// <summary>
        /// Verifies that GetByCardAsync throws an ArgumentException
        /// when the card identifier is empty.
        /// </summary>
        [Fact]
        public async Task GetByCardAsync_ShouldThrowArgumentException_WhenCardIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetByCardAsync(
                    Guid.Empty,
                    userId));

            // Assert
            Assert.Equal("CardId cannot be empty.", exception.Message);

            _cardRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.GetByCardAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that GetByCardAsync throws a KeyNotFoundException
        /// when the specified card does not exist.
        /// </summary>
        [Fact]
        public async Task GetByCardAsync_ShouldThrowKeyNotFoundException_WhenCardDoesNotExist()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync((Card?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.GetByCardAsync(
                    cardId,
                    userId));

            // Assert
            Assert.Equal("Card not found.", exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardBoardMemberAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.GetByCardAsync(cardId),
                Times.Never);
        }

        /// <summary>
        /// Verifies that GetByCardAsync returns the checklist tasks
        /// associated with the requested card.
        /// </summary>
        [Fact]
        public async Task GetByCardAsync_ShouldReturnCardTasks_WhenCardExists()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Test card");

            var firstTask = new CardTask(
                cardId,
                "First task",
                0);

            var secondTask = new CardTask(
                cardId,
                "Second task",
                1);

            secondTask.MarkAsCompleted();

            var tasks = new List<CardTask>
            {
                firstTask,
                secondTask
            };

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardBoardMemberAsync(
                    cardId,
                    userId))
                .Returns(Task.CompletedTask);

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByCardAsync(cardId))
                .ReturnsAsync(tasks);

            // Act
            var result = (
                await _service.GetByCardAsync(
                    cardId,
                    userId))
                .ToList();

            // Assert
            Assert.Equal(2, result.Count);

            Assert.Equal(firstTask.Id, result[0].Id);
            Assert.Equal(cardId, result[0].CardId);
            Assert.Equal("First task", result[0].Title);
            Assert.False(result[0].IsCompleted);
            Assert.Equal(0, result[0].Position);

            Assert.Equal(secondTask.Id, result[1].Id);
            Assert.Equal("Second task", result[1].Title);
            Assert.True(result[1].IsCompleted);
            Assert.Equal(1, result[1].Position);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardBoardMemberAsync(
                    cardId,
                    userId),
                Times.Once);

            _cardTaskRepositoryMock.Verify(
                repository => repository.GetByCardAsync(cardId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that GetByCardAsync does not access checklist
        /// task data when authorization fails.
        /// </summary>
        [Fact]
        public async Task GetByCardAsync_ShouldNotGetTasks_WhenAccessIsForbidden()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var cardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var card = new Card(
                boardId,
                "Test card");

            _cardRepositoryMock
                .Setup(repository => repository.GetByIdAsync(cardId))
                .ReturnsAsync(card);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardBoardMemberAsync(
                    cardId,
                    userId))
                .ThrowsAsync(
                    new ForbiddenAccessException("Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.GetByCardAsync(
                    cardId,
                    userId));

            // Assert
            _cardTaskRepositoryMock.Verify(
                repository => repository.GetByCardAsync(cardId),
                Times.Never);
        }

        // ============================================================
        // MARK AS COMPLETED
        // ============================================================

        /// <summary>
        /// Verifies that MarkAsCompletedAsync throws an ArgumentException
        /// when the task identifier is empty.
        /// </summary>
        [Fact]
        public async Task MarkAsCompletedAsync_ShouldThrowArgumentException_WhenTaskIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.MarkAsCompletedAsync(
                    Guid.Empty,
                    userId));

            // Assert
            Assert.Equal("TaskId cannot be empty.", exception.Message);

            _cardTaskRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that MarkAsCompletedAsync throws a KeyNotFoundException
        /// when the checklist task does not exist.
        /// </summary>
        [Fact]
        public async Task MarkAsCompletedAsync_ShouldThrowKeyNotFoundException_WhenTaskDoesNotExist()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByIdAsync(taskId))
                .ReturnsAsync((CardTask?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.MarkAsCompletedAsync(
                    taskId,
                    userId));

            // Assert
            Assert.Equal("Task not found.", exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardTaskBoardMemberAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that MarkAsCompletedAsync marks an existing
        /// checklist task as completed and persists the change.
        /// </summary>
        [Fact]
        public async Task MarkAsCompletedAsync_ShouldMarkTaskAsCompleted()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var task = new CardTask(
                cardId,
                "Task to complete");

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByIdAsync(taskId))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.MarkAsCompletedAsync(
                taskId,
                userId);

            // Assert
            Assert.True(task.IsCompleted);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId),
                Times.Once);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that MarkAsCompletedAsync does not modify or persist
        /// the checklist task when authorization fails.
        /// </summary>
        [Fact]
        public async Task MarkAsCompletedAsync_ShouldNotCompleteTask_WhenAccessIsForbidden()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var task = new CardTask(
                cardId,
                "Protected task");

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByIdAsync(taskId))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId))
                .ThrowsAsync(
                    new ForbiddenAccessException("Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.MarkAsCompletedAsync(
                    taskId,
                    userId));

            // Assert
            Assert.False(task.IsCompleted);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // MARK AS PENDING
        // ============================================================

        /// <summary>
        /// Verifies that MarkAsPendingAsync throws an ArgumentException
        /// when the task identifier is empty.
        /// </summary>
        [Fact]
        public async Task MarkAsPendingAsync_ShouldThrowArgumentException_WhenTaskIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.MarkAsPendingAsync(
                    Guid.Empty,
                    userId));

            // Assert
            Assert.Equal("TaskId cannot be empty.", exception.Message);

            _cardTaskRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that MarkAsPendingAsync throws a KeyNotFoundException
        /// when the checklist task does not exist.
        /// </summary>
        [Fact]
        public async Task MarkAsPendingAsync_ShouldThrowKeyNotFoundException_WhenTaskDoesNotExist()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByIdAsync(taskId))
                .ReturnsAsync((CardTask?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.MarkAsPendingAsync(
                    taskId,
                    userId));

            // Assert
            Assert.Equal("Task not found.", exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardTaskBoardMemberAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that MarkAsPendingAsync marks an existing completed
        /// checklist task as pending and persists the change.
        /// </summary>
        [Fact]
        public async Task MarkAsPendingAsync_ShouldMarkTaskAsPending()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var task = new CardTask(
                cardId,
                "Completed task");

            task.MarkAsCompleted();

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByIdAsync(taskId))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.MarkAsPendingAsync(
                taskId,
                userId);

            // Assert
            Assert.False(task.IsCompleted);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId),
                Times.Once);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that MarkAsPendingAsync does not modify or persist
        /// the checklist task when authorization fails.
        /// </summary>
        [Fact]
        public async Task MarkAsPendingAsync_ShouldNotMarkTaskAsPending_WhenAccessIsForbidden()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var task = new CardTask(
                cardId,
                "Protected completed task");

            task.MarkAsCompleted();

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByIdAsync(taskId))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId))
                .ThrowsAsync(
                    new ForbiddenAccessException("Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.MarkAsPendingAsync(
                    taskId,
                    userId));

            // Assert
            Assert.True(task.IsCompleted);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // REMOVE
        // ============================================================

        /// <summary>
        /// Verifies that RemoveAsync throws an ArgumentException
        /// when the task identifier is empty.
        /// </summary>
        [Fact]
        public async Task RemoveAsync_ShouldThrowArgumentException_WhenTaskIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.RemoveAsync(
                    Guid.Empty,
                    userId));

            // Assert
            Assert.Equal("TaskId cannot be empty.", exception.Message);

            _cardTaskRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.RemoveAsync(It.IsAny<CardTask>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that RemoveAsync throws a KeyNotFoundException
        /// when the checklist task does not exist.
        /// </summary>
        [Fact]
        public async Task RemoveAsync_ShouldThrowKeyNotFoundException_WhenTaskDoesNotExist()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByIdAsync(taskId))
                .ReturnsAsync((CardTask?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.RemoveAsync(
                    taskId,
                    userId));

            // Assert
            Assert.Equal("Task not found.", exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardTaskBoardMemberAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.RemoveAsync(It.IsAny<CardTask>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that RemoveAsync removes an existing checklist
        /// task and persists the change.
        /// </summary>
        [Fact]
        public async Task RemoveAsync_ShouldRemoveTask_WhenTaskExists()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var task = new CardTask(
                cardId,
                "Task to remove");

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByIdAsync(taskId))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.RemoveAsync(
                taskId,
                userId);

            // Assert
            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId),
                Times.Once);

            _cardTaskRepositoryMock.Verify(
                repository => repository.RemoveAsync(task),
                Times.Once);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that RemoveAsync does not remove or persist
        /// the checklist task when authorization fails.
        /// </summary>
        [Fact]
        public async Task RemoveAsync_ShouldNotRemoveTask_WhenAccessIsForbidden()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var task = new CardTask(
                cardId,
                "Protected task");

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByIdAsync(taskId))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId))
                .ThrowsAsync(
                    new ForbiddenAccessException("Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.RemoveAsync(
                    taskId,
                    userId));

            // Assert
            _cardTaskRepositoryMock.Verify(
                repository => repository.RemoveAsync(It.IsAny<CardTask>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // UPDATE
        // ============================================================

        /// <summary>
        /// Verifies that UpdateAsync throws an ArgumentException
        /// when the task identifier is empty.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldThrowArgumentException_WhenTaskIdIsEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var request = new UpdateCardTaskRequest
            {
                Title = "Updated task"
            };

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateAsync(
                    Guid.Empty,
                    userId,
                    request));

            // Assert
            Assert.Equal("TaskId cannot be empty.", exception.Message);

            _cardTaskRepositoryMock.Verify(
                repository => repository.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that UpdateAsync throws a KeyNotFoundException
        /// when the checklist task does not exist.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldThrowKeyNotFoundException_WhenTaskDoesNotExist()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var request = new UpdateCardTaskRequest
            {
                Title = "Updated task"
            };

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByIdAsync(taskId))
                .ReturnsAsync((CardTask?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateAsync(
                    taskId,
                    userId,
                    request));

            // Assert
            Assert.Equal("Task not found.", exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardTaskBoardMemberAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>()),
                Times.Never);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that UpdateAsync changes the title of
        /// an existing checklist task and persists the change.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateTitle_WhenTaskExists()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var task = new CardTask(
                cardId,
                "Original title");

            var request = new UpdateCardTaskRequest
            {
                Title = "Updated title"
            };

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByIdAsync(taskId))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAsync(
                taskId,
                userId,
                request);

            // Assert
            Assert.Equal("Updated title", task.Title);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId),
                Times.Once);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that UpdateAsync trims whitespace
        /// from a supplied checklist task title.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldTrimTitle()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var task = new CardTask(
                cardId,
                "Original title");

            var request = new UpdateCardTaskRequest
            {
                Title = "   Updated title   "
            };

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByIdAsync(taskId))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAsync(
                taskId,
                userId,
                request);

            // Assert
            Assert.Equal("Updated title", task.Title);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that UpdateAsync keeps the current title
        /// when a blank title is supplied.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldKeepTitle_WhenTitleIsBlank()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var task = new CardTask(
                cardId,
                "Original title");

            var request = new UpdateCardTaskRequest
            {
                Title = "   "
            };

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByIdAsync(taskId))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAsync(
                taskId,
                userId,
                request);

            // Assert
            Assert.Equal("Original title", task.Title);

            _membershipAuthorizationServiceMock.Verify(
                service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId),
                Times.Once);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that UpdateAsync does not modify or persist
        /// the checklist task when authorization fails.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldNotUpdateTask_WhenAccessIsForbidden()
        {
            // Arrange
            var cardId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var task = new CardTask(
                cardId,
                "Original title");

            var request = new UpdateCardTaskRequest
            {
                Title = "Unauthorized update"
            };

            _cardTaskRepositoryMock
                .Setup(repository => repository.GetByIdAsync(taskId))
                .ReturnsAsync(task);

            _membershipAuthorizationServiceMock
                .Setup(service => service.EnsureCardTaskBoardMemberAsync(
                    taskId,
                    userId))
                .ThrowsAsync(
                    new ForbiddenAccessException("Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _service.UpdateAsync(
                    taskId,
                    userId,
                    request));

            // Assert
            Assert.Equal("Original title", task.Title);

            _cardTaskRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }
    }
}