using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SprintBoard.api.Controllers;
using SprintBoard.api.Services;
using SprintBoard.Application.DTOs.Board;
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
    /// Contains tests for the <see cref="BoardsController"/>.
    /// </summary>
    public class BoardsControllerTests
    {
        private readonly Mock<IBoardRepository> _boardRepositoryMock;
        private readonly Mock<ICardRepository> _cardRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IBoardMemberRepository> _boardMemberRepositoryMock;
        private readonly Mock<IBoardInvitationRepository> _boardInvitationRepositoryMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IMembershipAuthorizationService> _membershipAuthorizationServiceMock;
        private readonly Mock<IInvitationLinkBuilder> _invitationLinkBuilderMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;

        private readonly BoardService _boardService;
        private readonly CardService _cardService;
        private readonly BoardsController _controller;

        /// <summary>
        /// Initializes the mocked dependencies, application services,
        /// and controller instance used by the board controller tests.
        /// </summary>
        public BoardsControllerTests()
        {
            _boardRepositoryMock =
                new Mock<IBoardRepository>();

            _cardRepositoryMock =
                new Mock<ICardRepository>();

            _userRepositoryMock =
                new Mock<IUserRepository>();

            _boardMemberRepositoryMock =
                new Mock<IBoardMemberRepository>();

            _boardInvitationRepositoryMock =
                new Mock<IBoardInvitationRepository>();

            _emailServiceMock =
                new Mock<IEmailService>();

            _membershipAuthorizationServiceMock =
                new Mock<IMembershipAuthorizationService>();

            _invitationLinkBuilderMock =
                new Mock<IInvitationLinkBuilder>();

            _currentUserServiceMock =
                new Mock<ICurrentUserService>();

            _boardService = new BoardService(
                _boardRepositoryMock.Object,
                _userRepositoryMock.Object,
                _boardMemberRepositoryMock.Object,
                _boardInvitationRepositoryMock.Object,
                _emailServiceMock.Object,
                _membershipAuthorizationServiceMock.Object,
                _invitationLinkBuilderMock.Object);

            _cardService = new CardService(
                _boardRepositoryMock.Object,
                _cardRepositoryMock.Object,
                _membershipAuthorizationServiceMock.Object);

            _controller = new BoardsController(
                _boardService,
                _cardService,
                _currentUserServiceMock.Object);
        }

        // ============================================================
        // CREATE BOARD
        // ============================================================

        /// <summary>
        /// Verifies that Create returns HTTP 201 containing the
        /// newly created board and references the GetById action.
        /// </summary>
        [Fact]
        public async Task Create_ShouldReturnCreatedAtAction_WhenRequestIsValid()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var request = new CreateBoardRequest
            {
                Name = "   Sprint Board   "
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            // Act
            var result =
                await _controller.Create(request);

            // Assert
            var createdResult =
                Assert.IsType<CreatedAtActionResult>(
                    result.Result);

            Assert.Equal(
                StatusCodes.Status201Created,
                createdResult.StatusCode);

            Assert.Equal(
                nameof(BoardsController.GetById),
                createdResult.ActionName);

            var response =
                Assert.IsType<BoardResponse>(
                    createdResult.Value);

            Assert.NotEqual(
                Guid.Empty,
                response.Id);

            Assert.Equal(
                "Sprint Board",
                response.Name);

            Assert.Equal(
                userId,
                response.OwnerId);

            Assert.NotNull(
                createdResult.RouteValues);

            Assert.Equal(
                response.Id,
                createdResult.RouteValues!["id"]);
        }

        /// <summary>
        /// Verifies that Create uses the authenticated user as the board
        /// owner and persists both the board and owner membership.
        /// </summary>
        [Fact]
        public async Task Create_ShouldPersistBoardAndOwnerMembership()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var request = new CreateBoardRequest
            {
                Name = "Team Board"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            // Act
            var result =
                await _controller.Create(request);

            // Assert
            var createdResult =
                Assert.IsType<CreatedAtActionResult>(
                    result.Result);

            var response =
                Assert.IsType<BoardResponse>(
                    createdResult.Value);

            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _boardRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<Board>(board =>
                        board.OwnerId == userId &&
                        board.Name == "Team Board")),
                Times.Once);

            _boardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<BoardMember>(member =>
                        member.BoardId == response.Id &&
                        member.UserId == userId &&
                        member.Role == BoardRole.Owner)),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Create propagates an UnauthorizedAccessException
        /// and does not persist a board when no authenticated user exists.
        /// </summary>
        [Fact]
        public async Task Create_ShouldPropagateUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var request = new CreateBoardRequest
            {
                Name = "Board"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Throws(
                    new UnauthorizedAccessException(
                        "User is not authenticated."));

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => _controller.Create(request));

            // Assert
            Assert.Equal(
                "User is not authenticated.",
                exception.Message);

            _boardRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Board>()),
                Times.Never);

            _boardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<BoardMember>()),
                Times.Never);
        }

        // ============================================================
        // CREATE CARD
        // ============================================================

        /// <summary>
        /// Verifies that CreateCard returns HTTP 201 with the newly
        /// created card and the expected resource location.
        /// </summary>
        [Fact]
        public async Task CreateCard_ShouldReturnCreated_WhenRequestIsValid()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var board = new Board(
                "Test Board",
                Guid.NewGuid());

            var request = new CreateCardRequest
            {
                Title = "   New Card   ",
                Description = "   Card description   ",
                Position = 2
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(board.Id))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardMemberAsync(
                        board.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            var result =
                await _controller.CreateCard(
                    board.Id,
                    request);

            // Assert
            var createdResult =
                Assert.IsType<CreatedResult>(
                    result.Result);

            Assert.Equal(
                StatusCodes.Status201Created,
                createdResult.StatusCode);

            var response =
                Assert.IsType<CardResponse>(
                    createdResult.Value);

            Assert.Equal(
                board.Id,
                response.BoardId);

            Assert.Equal(
                "New Card",
                response.Title);

            Assert.Equal(
                "Card description",
                response.Description);

            Assert.Equal(
                2,
                response.Position);

            Assert.Equal(
                CardStatus.ToDo,
                response.Status);

            Assert.Equal(
                $"/api/v1/boards/{board.Id}/cards/{response.Id}",
                createdResult.Location);
        }

        /// <summary>
        /// Verifies that CreateCard uses the authenticated user for
        /// authorization and persists the newly created card.
        /// </summary>
        [Fact]
        public async Task CreateCard_ShouldUseAuthenticatedUserAndPersistCard()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var board = new Board(
                "Test Board",
                Guid.NewGuid());

            var request = new CreateCardRequest
            {
                Title = "New Card"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(board.Id))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardMemberAsync(
                        board.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.CreateCard(
                board.Id,
                request);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureBoardMemberAsync(
                        board.Id,
                        userId),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<Card>(card =>
                        card.BoardId == board.Id &&
                        card.Title == "New Card")),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that CreateCard propagates a KeyNotFoundException
        /// when the requested parent board does not exist.
        /// </summary>
        [Fact]
        public async Task CreateCard_ShouldPropagateKeyNotFoundException_WhenBoardDoesNotExist()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var request = new CreateCardRequest
            {
                Title = "New Card"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => _controller.CreateCard(
                        boardId,
                        request));

            // Assert
            Assert.Equal(
                "Board not found.",
                exception.Message);

            _cardRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Card>()),
                Times.Never);

            _cardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that CreateCard propagates a ForbiddenAccessException
        /// and does not create a card when board access is denied.
        /// </summary>
        [Fact]
        public async Task CreateCard_ShouldPropagateForbiddenAccessException_WhenAccessIsDenied()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var board = new Board(
                "Protected Board",
                Guid.NewGuid());

            var request = new CreateCardRequest
            {
                Title = "Unauthorized Card"
            };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(board.Id))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardMemberAsync(
                        board.Id,
                        userId))
                .ThrowsAsync(
                    new ForbiddenAccessException(
                        "Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _controller.CreateCard(
                    board.Id,
                    request));

            // Assert
            _cardRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.IsAny<Card>()),
                Times.Never);

            _cardRepositoryMock.Verify(
                repository => repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // GET CARDS
        // ============================================================

        /// <summary>
        /// Verifies that GetCards returns HTTP 200 containing all cards
        /// ordered by workflow status as defined by CardService.
        /// </summary>
        [Fact]
        public async Task GetCards_ShouldReturnOkWithCardsOrderedByStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var board = new Board(
                "Test Board",
                Guid.NewGuid());

            var todoCard = new Card(
                board.Id,
                "To Do");

            var doingCard = new Card(
                board.Id,
                "Doing");

            doingCard.ChangeStatus(
                CardStatus.Doing);

            var doneCard = new Card(
                board.Id,
                "Done");

            doneCard.ChangeStatus(
                CardStatus.Done);

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(board.Id))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardMemberAsync(
                        board.Id,
                        userId))
                .Returns(Task.CompletedTask);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByBoardAsync(board.Id))
                .ReturnsAsync(
                    new[]
                    {
                        todoCard,
                        doneCard,
                        doingCard
                    });

            // Act
            var result =
                await _controller.GetCards(board.Id);

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(
                    result.Result);

            Assert.Equal(
                StatusCodes.Status200OK,
                okResult.StatusCode);

            var cards =
                Assert.IsAssignableFrom<
                    IEnumerable<CardResponse>>(
                    okResult.Value)
                    .ToList();

            Assert.Equal(
                3,
                cards.Count);

            Assert.Equal(
                CardStatus.Done,
                cards[0].Status);

            Assert.Equal(
                CardStatus.Doing,
                cards[1].Status);

            Assert.Equal(
                CardStatus.ToDo,
                cards[2].Status);
        }

        /// <summary>
        /// Verifies that GetCards returns an empty collection when
        /// an existing board does not contain any cards.
        /// </summary>
        [Fact]
        public async Task GetCards_ShouldReturnEmptyCollection_WhenBoardHasNoCards()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var board = new Board(
                "Empty Board",
                Guid.NewGuid());

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(board.Id))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardMemberAsync(
                        board.Id,
                        userId))
                .Returns(Task.CompletedTask);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByBoardAsync(board.Id))
                .ReturnsAsync(
                    Array.Empty<Card>());

            // Act
            var result =
                await _controller.GetCards(board.Id);

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(
                    result.Result);

            var cards =
                Assert.IsAssignableFrom<
                    IEnumerable<CardResponse>>(
                    okResult.Value);

            Assert.Empty(cards);
        }

        /// <summary>
        /// Verifies that GetCards uses the authenticated user's
        /// identifier when checking board membership.
        /// </summary>
        [Fact]
        public async Task GetCards_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var board = new Board(
                "Test Board",
                Guid.NewGuid());

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(board.Id))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardMemberAsync(
                        board.Id,
                        userId))
                .Returns(Task.CompletedTask);

            _cardRepositoryMock
                .Setup(repository =>
                    repository.GetByBoardAsync(board.Id))
                .ReturnsAsync(
                    Array.Empty<Card>());

            // Act
            await _controller.GetCards(board.Id);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureBoardMemberAsync(
                        board.Id,
                        userId),
                Times.Once);

            _cardRepositoryMock.Verify(
                repository =>
                    repository.GetByBoardAsync(board.Id),
                Times.Once);
        }

        /// <summary>
        /// Verifies that GetCards propagates a ForbiddenAccessException
        /// and does not retrieve cards when board access is denied.
        /// </summary>
        [Fact]
        public async Task GetCards_ShouldPropagateForbiddenAccessException_WhenAccessIsDenied()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var board = new Board(
                "Protected Board",
                Guid.NewGuid());

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(board.Id))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardMemberAsync(
                        board.Id,
                        userId))
                .ThrowsAsync(
                    new ForbiddenAccessException(
                        "Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _controller.GetCards(
                    board.Id));

            // Assert
            _cardRepositoryMock.Verify(
                repository =>
                    repository.GetByBoardAsync(
                        It.IsAny<Guid>()),
                Times.Never);
        }

        // ============================================================
        // GET BOARD BY ID
        // ============================================================

        /// <summary>
        /// Verifies that GetById returns HTTP 200 containing
        /// the requested board when the user has access.
        /// </summary>
        [Fact]
        public async Task GetById_ShouldReturnOkWithBoard_WhenUserIsMember()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var board = new Board(
                "My Board",
                Guid.NewGuid());

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(board.Id))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardMemberAsync(
                        board.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            var result =
                await _controller.GetById(
                    board.Id);

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(
                    result.Result);

            Assert.Equal(
                StatusCodes.Status200OK,
                okResult.StatusCode);

            var response =
                Assert.IsType<BoardResponse>(
                    okResult.Value);

            Assert.Equal(
                board.Id,
                response.Id);

            Assert.Equal(
                "My Board",
                response.Name);

            Assert.Equal(
                board.OwnerId,
                response.OwnerId);
        }

        /// <summary>
        /// Verifies that GetById uses the authenticated user's
        /// identifier when authorizing access to the board.
        /// </summary>
        [Fact]
        public async Task GetById_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var board = new Board(
                "Board",
                Guid.NewGuid());

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(board.Id))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardMemberAsync(
                        board.Id,
                        userId))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.GetById(
                board.Id);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureBoardMemberAsync(
                        board.Id,
                        userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that GetById propagates a KeyNotFoundException
        /// when the requested board does not exist.
        /// </summary>
        [Fact]
        public async Task GetById_ShouldPropagateKeyNotFoundException_WhenBoardDoesNotExist()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => _controller.GetById(
                        boardId));

            // Assert
            Assert.Equal(
                "Board not found.",
                exception.Message);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureBoardMemberAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that GetById propagates a ForbiddenAccessException
        /// when the authenticated user is not a member of the board.
        /// </summary>
        [Fact]
        public async Task GetById_ShouldPropagateForbiddenAccessException_WhenAccessIsDenied()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var board = new Board(
                "Protected Board",
                Guid.NewGuid());

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(board.Id))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardMemberAsync(
                        board.Id,
                        userId))
                .ThrowsAsync(
                    new ForbiddenAccessException(
                        "Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _controller.GetById(
                    board.Id));

            // Assert
            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureBoardMemberAsync(
                        board.Id,
                        userId),
                Times.Once);
        }

        // ============================================================
        // GET MY BOARDS
        // ============================================================

        /// <summary>
        /// Verifies that GetMyBoards returns HTTP 200 containing
        /// all boards accessible to the authenticated user.
        /// </summary>
        [Fact]
        public async Task GetMyBoards_ShouldReturnOkWithUserBoards()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var firstBoard = new Board(
                "First Board",
                userId);

            var secondBoard = new Board(
                "Second Board",
                Guid.NewGuid());

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByUserMembershipAsync(
                        userId))
                .ReturnsAsync(
                    new[]
                    {
                        firstBoard,
                        secondBoard
                    });

            // Act
            var result =
                await _controller.GetMyBoards();

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(
                    result.Result);

            Assert.Equal(
                StatusCodes.Status200OK,
                okResult.StatusCode);

            var boards =
                Assert.IsAssignableFrom<
                    IEnumerable<BoardResponse>>(
                    okResult.Value)
                    .ToList();

            Assert.Equal(
                2,
                boards.Count);

            Assert.Equal(
                firstBoard.Id,
                boards[0].Id);

            Assert.Equal(
                "First Board",
                boards[0].Name);

            Assert.Equal(
                secondBoard.Id,
                boards[1].Id);

            Assert.Equal(
                "Second Board",
                boards[1].Name);

            _boardRepositoryMock.Verify(
                repository =>
                    repository.GetByUserMembershipAsync(
                        userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that GetMyBoards returns an empty collection
        /// when the authenticated user has no board memberships.
        /// </summary>
        [Fact]
        public async Task GetMyBoards_ShouldReturnEmptyCollection_WhenUserHasNoBoards()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByUserMembershipAsync(
                        userId))
                .ReturnsAsync(
                    Array.Empty<Board>());

            // Act
            var result =
                await _controller.GetMyBoards();

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(
                    result.Result);

            var boards =
                Assert.IsAssignableFrom<
                    IEnumerable<BoardResponse>>(
                    okResult.Value);

            Assert.Empty(boards);
        }

        /// <summary>
        /// Verifies that GetMyBoards propagates an
        /// UnauthorizedAccessException when no authenticated user exists.
        /// </summary>
        [Fact]
        public async Task GetMyBoards_ShouldPropagateUnauthorizedAccessException_WhenUserIsNotAuthenticated()
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
                    () => _controller.GetMyBoards());

            // Assert
            Assert.Equal(
                "User is not authenticated.",
                exception.Message);

            _boardRepositoryMock.Verify(
                repository =>
                    repository.GetByUserMembershipAsync(
                        It.IsAny<Guid>()),
                Times.Never);
        }
    }
}