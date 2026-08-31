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

        // ============================================================
        // CREATE INVITATION
        // ============================================================

        /// <summary>
        /// Verifies that CreateInvitation returns HTTP 200 containing
        /// the newly created board invitation.
        /// </summary>
        [Fact]
        public async Task CreateInvitation_ShouldReturnOk_WhenRequestIsValid()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var board = new Board(
                "Team Board",
                userId);

            var request = new CreateBoardInvitationRequest
            {
                Email = " Candidate@Example.com "
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
                    service.EnsureBoardOwnerOrAdminAsync(
                        board.Id,
                        userId))
                .Returns(Task.CompletedTask);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync(
                        "candidate@example.com"))
                .ReturnsAsync((User?)null);

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.ExistsPendingAsync(
                        board.Id,
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

            // Act
            var result =
                await _controller.CreateInvitation(
                    board.Id,
                    request);

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(
                    result.Result);

            Assert.Equal(
                StatusCodes.Status200OK,
                okResult.StatusCode);

            var response =
                Assert.IsType<BoardInvitationResponse>(
                    okResult.Value);

            Assert.Equal(
                board.Id,
                response.BoardId);

            Assert.Equal(
                "candidate@example.com",
                response.Email);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    response.Token));
        }

        /// <summary>
        /// Verifies that CreateInvitation uses the authenticated user
        /// as the requester and sends the invitation email.
        /// </summary>
        [Fact]
        public async Task CreateInvitation_ShouldUseAuthenticatedUserAndSendEmail()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var board = new Board(
                "Team Board",
                userId);

            var request = new CreateBoardInvitationRequest
            {
                Email = "candidate@example.com"
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
                    service.EnsureBoardOwnerOrAdminAsync(
                        board.Id,
                        userId))
                .Returns(Task.CompletedTask);

            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByEmailAsync(
                        "candidate@example.com"))
                .ReturnsAsync((User?)null);

            _boardInvitationRepositoryMock
                .Setup(repository =>
                    repository.ExistsPendingAsync(
                        board.Id,
                        "candidate@example.com"))
                .ReturnsAsync(false);

            _invitationLinkBuilderMock
                .Setup(builder =>
                    builder.BuildAcceptInvitationLink(
                        It.IsAny<string>()))
                .Returns("https://test/accept");

            _invitationLinkBuilderMock
                .Setup(builder =>
                    builder.BuildDeclineInvitationLink(
                        It.IsAny<string>()))
                .Returns("https://test/decline");

            // Act
            await _controller.CreateInvitation(
                board.Id,
                request);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureBoardOwnerOrAdminAsync(
                        board.Id,
                        userId),
                Times.Once);

            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.Is<BoardInvitation>(
                            invitation =>
                                invitation.BoardId == board.Id &&
                                invitation.InvitedByUserId == userId &&
                                invitation.Email ==
                                    "candidate@example.com")),
                Times.Once);

            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);

            _emailServiceMock.Verify(
                service =>
                    service.SendBoardInvitationAsync(
                        "candidate@example.com",
                        "Team Board",
                        "https://test/accept",
                        "https://test/decline"),
                Times.Once);
        }

        /// <summary>
        /// Verifies that CreateInvitation propagates a
        /// KeyNotFoundException when the board does not exist.
        /// </summary>
        [Fact]
        public async Task CreateInvitation_ShouldPropagateKeyNotFoundException_WhenBoardDoesNotExist()
        {
            // Arrange
            var boardId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(Guid.NewGuid());

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            var request =
                new CreateBoardInvitationRequest
                {
                    Email = "user@example.com"
                };

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => _controller.CreateInvitation(
                        boardId,
                        request));

            // Assert
            Assert.Equal(
                "Board not found.",
                exception.Message);

            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<BoardInvitation>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that CreateInvitation propagates a
        /// ForbiddenAccessException when the requester lacks permission.
        /// </summary>
        [Fact]
        public async Task CreateInvitation_ShouldPropagateForbiddenAccessException_WhenAccessIsDenied()
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
                    service.EnsureBoardOwnerOrAdminAsync(
                        board.Id,
                        userId))
                .ThrowsAsync(
                    new ForbiddenAccessException(
                        "Access denied."));

            var request =
                new CreateBoardInvitationRequest
                {
                    Email = "user@example.com"
                };

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _controller.CreateInvitation(
                    board.Id,
                    request));

            // Assert
            _boardInvitationRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<BoardInvitation>()),
                Times.Never);

            _emailServiceMock.Verify(
                service =>
                    service.SendBoardInvitationAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                Times.Never);
        }

        // ============================================================
        // CHANGE ROLE
        // ============================================================

        /// <summary>
        /// Verifies that ChangeRole returns HTTP 204 and changes
        /// the requested member's role.
        /// </summary>
        [Fact]
        public async Task ChangeRole_ShouldReturnNoContent_WhenRequestIsValid()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var membership =
                new BoardMember(
                    boardId,
                    memberId,
                    BoardRole.Member);

            var request =
                new ChangeBoardMemberRoleRequest
                {
                    MemberUserId = memberId,
                    NewRole = (int)BoardRole.Admin
                };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(ownerId);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardOwnerAsync(
                        boardId,
                        ownerId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        memberId))
                .ReturnsAsync(membership);

            // Act
            var result =
                await _controller.ChangeRole(
                    boardId,
                    request);

            // Assert
            Assert.IsType<NoContentResult>(result);

            Assert.Equal(
                BoardRole.Admin,
                membership.Role);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that ChangeRole uses the authenticated user
        /// as the requester of the role change.
        /// </summary>
        [Fact]
        public async Task ChangeRole_ShouldUseAuthenticatedUserAsRequester()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var membership =
                new BoardMember(
                    boardId,
                    memberId,
                    BoardRole.Member);

            var request =
                new ChangeBoardMemberRoleRequest
                {
                    MemberUserId = memberId,
                    NewRole = (int)BoardRole.Admin
                };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(ownerId);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardOwnerAsync(
                        boardId,
                        ownerId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        memberId))
                .ReturnsAsync(membership);

            // Act
            await _controller.ChangeRole(
                boardId,
                request);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureBoardOwnerAsync(
                        boardId,
                        ownerId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that ChangeRole propagates a ForbiddenAccessException
        /// when the requester is not the board owner.
        /// </summary>
        [Fact]
        public async Task ChangeRole_ShouldPropagateForbiddenAccessException_WhenRequesterIsNotOwner()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();

            var request =
                new ChangeBoardMemberRoleRequest
                {
                    MemberUserId = Guid.NewGuid(),
                    NewRole = (int)BoardRole.Admin
                };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(requesterId);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardOwnerAsync(
                        boardId,
                        requesterId))
                .ThrowsAsync(
                    new ForbiddenAccessException(
                        "Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => _controller.ChangeRole(
                    boardId,
                    request));

            // Assert
            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.GetMemberAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>()),
                Times.Never);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that ChangeRole propagates an InvalidOperationException
        /// when an attempt is made to change the owner's role.
        /// </summary>
        [Fact]
        public async Task ChangeRole_ShouldPropagateInvalidOperationException_WhenTargetIsOwner()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();

            var ownerMembership =
                new BoardMember(
                    boardId,
                    ownerId,
                    BoardRole.Owner);

            var request =
                new ChangeBoardMemberRoleRequest
                {
                    MemberUserId = ownerId,
                    NewRole = (int)BoardRole.Admin
                };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(requesterId);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardOwnerAsync(
                        boardId,
                        requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        ownerId))
                .ReturnsAsync(ownerMembership);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _controller.ChangeRole(
                        boardId,
                        request));

            // Assert
            Assert.Equal(
                "Cannot change the owner's role.",
                exception.Message);

            Assert.Equal(
                BoardRole.Owner,
                ownerMembership.Role);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // REMOVE MEMBER
        // ============================================================

        /// <summary>
        /// Verifies that RemoveMember returns HTTP 204 when
        /// an authorized owner removes a board member.
        /// </summary>
        [Fact]
        public async Task RemoveMember_ShouldReturnNoContent_WhenOwnerRemovesMember()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var board =
                new Board(
                    "Board",
                    ownerId);

            var ownerMembership =
                new BoardMember(
                    boardId,
                    ownerId,
                    BoardRole.Owner);

            var memberMembership =
                new BoardMember(
                    boardId,
                    memberId,
                    BoardRole.Member);

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(ownerId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardOwnerOrAdminAsync(
                        boardId,
                        ownerId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        ownerId))
                .ReturnsAsync(ownerMembership);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        memberId))
                .ReturnsAsync(memberMembership);

            // Act
            var result =
                await _controller.RemoveMember(
                    boardId,
                    memberId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(
                        memberMembership),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that RemoveMember uses the authenticated user
        /// as the requester of the removal operation.
        /// </summary>
        [Fact]
        public async Task RemoveMember_ShouldUseAuthenticatedUserAsRequester()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var board =
                new Board(
                    "Board",
                    ownerId);

            var ownerMembership =
                new BoardMember(
                    boardId,
                    ownerId,
                    BoardRole.Owner);

            var memberMembership =
                new BoardMember(
                    boardId,
                    memberId,
                    BoardRole.Member);

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(ownerId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardOwnerOrAdminAsync(
                        boardId,
                        ownerId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        ownerId))
                .ReturnsAsync(ownerMembership);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        memberId))
                .ReturnsAsync(memberMembership);

            // Act
            await _controller.RemoveMember(
                boardId,
                memberId);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureBoardOwnerOrAdminAsync(
                        boardId,
                        ownerId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that RemoveMember propagates a ForbiddenAccessException
        /// when an administrator attempts to remove another administrator.
        /// </summary>
        [Fact]
        public async Task RemoveMember_ShouldPropagateForbiddenAccessException_WhenAdminRemovesAdmin()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var targetAdminId = Guid.NewGuid();

            var board =
                new Board(
                    "Board",
                    Guid.NewGuid());

            var requesterMembership =
                new BoardMember(
                    boardId,
                    adminId,
                    BoardRole.Admin);

            var targetMembership =
                new BoardMember(
                    boardId,
                    targetAdminId,
                    BoardRole.Admin);

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(adminId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardOwnerOrAdminAsync(
                        boardId,
                        adminId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        adminId))
                .ReturnsAsync(requesterMembership);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        targetAdminId))
                .ReturnsAsync(targetMembership);

            // Act
            var exception =
                await Assert.ThrowsAsync<ForbiddenAccessException>(
                    () => _controller.RemoveMember(
                        boardId,
                        targetAdminId));

            // Assert
            Assert.Equal(
                "Administrators can only remove members with the Member role.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(
                        It.IsAny<BoardMember>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that RemoveMember propagates a KeyNotFoundException
        /// when the target membership cannot be found.
        /// </summary>
        [Fact]
        public async Task RemoveMember_ShouldPropagateKeyNotFoundException_WhenMemberDoesNotExist()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var board =
                new Board(
                    "Board",
                    ownerId);

            var ownerMembership =
                new BoardMember(
                    boardId,
                    ownerId,
                    BoardRole.Owner);

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(ownerId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardOwnerOrAdminAsync(
                        boardId,
                        ownerId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        ownerId))
                .ReturnsAsync(ownerMembership);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        memberId))
                .ReturnsAsync((BoardMember?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () => _controller.RemoveMember(
                        boardId,
                        memberId));

            // Assert
            Assert.Equal(
                "Member not found.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(
                        It.IsAny<BoardMember>()),
                Times.Never);
        }

        // ============================================================
        // LEAVE BOARD
        // ============================================================

        /// <summary>
        /// Verifies that LeaveBoard returns HTTP 204 and removes
        /// the authenticated member's board membership.
        /// </summary>
        [Fact]
        public async Task LeaveBoard_ShouldReturnNoContent_WhenMemberLeaves()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board =
                new Board(
                    "Board",
                    Guid.NewGuid());

            var membership =
                new BoardMember(
                    boardId,
                    userId,
                    BoardRole.Member);

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        userId))
                .ReturnsAsync(membership);

            // Act
            var result =
                await _controller.LeaveBoard(
                    boardId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(
                        membership),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that LeaveBoard uses the identifier of the
        /// currently authenticated user.
        /// </summary>
        [Fact]
        public async Task LeaveBoard_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board =
                new Board(
                    "Board",
                    Guid.NewGuid());

            var membership =
                new BoardMember(
                    boardId,
                    userId,
                    BoardRole.Member);

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        userId))
                .ReturnsAsync(membership);

            // Act
            await _controller.LeaveBoard(
                boardId);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.GetMemberAsync(
                        boardId,
                        userId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that LeaveBoard propagates an InvalidOperationException
        /// when the board owner attempts to leave the board.
        /// </summary>
        [Fact]
        public async Task LeaveBoard_ShouldPropagateInvalidOperationException_WhenOwnerTriesToLeave()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();

            var board =
                new Board(
                    "Board",
                    ownerId);

            var membership =
                new BoardMember(
                    boardId,
                    ownerId,
                    BoardRole.Owner);

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(ownerId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        ownerId))
                .ReturnsAsync(membership);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () =>
                        _controller.LeaveBoard(
                            boardId));

            // Assert
            Assert.Equal(
                "The board owner cannot leave the board. Delete the board instead.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(
                        It.IsAny<BoardMember>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that LeaveBoard propagates a KeyNotFoundException
        /// when the authenticated user has no membership on the board.
        /// </summary>
        [Fact]
        public async Task LeaveBoard_ShouldPropagateKeyNotFoundException_WhenMembershipDoesNotExist()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var board =
                new Board(
                    "Board",
                    Guid.NewGuid());

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(userId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(boardId))
                .ReturnsAsync(board);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMemberAsync(
                        boardId,
                        userId))
                .ReturnsAsync((BoardMember?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () =>
                        _controller.LeaveBoard(
                            boardId));

            // Assert
            Assert.Equal(
                "Board membership not found.",
                exception.Message);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(
                        It.IsAny<BoardMember>()),
                Times.Never);
        }

        // ============================================================
        // DELETE BOARD
        // ============================================================

        /// <summary>
        /// Verifies that Delete returns HTTP 204 and removes
        /// the board when requested by its owner.
        /// </summary>
        [Fact]
        public async Task Delete_ShouldReturnNoContent_WhenUserIsOwner()
        {
            // Arrange
            var ownerId = Guid.NewGuid();

            var board =
                new Board(
                    "Board to delete",
                    ownerId);

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(ownerId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(board.Id))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardOwnerAsync(
                        board.Id,
                        ownerId))
                .Returns(Task.CompletedTask);

            // Act
            var result =
                await _controller.Delete(
                    board.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _boardRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(board),
                Times.Once);

            _boardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Delete uses the authenticated user's
        /// identifier when verifying board ownership.
        /// </summary>
        [Fact]
        public async Task Delete_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var ownerId = Guid.NewGuid();

            var board =
                new Board(
                    "Board",
                    ownerId);

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(ownerId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(board.Id))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardOwnerAsync(
                        board.Id,
                        ownerId))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Delete(
                board.Id);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureBoardOwnerAsync(
                        board.Id,
                        ownerId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Delete propagates a KeyNotFoundException
        /// when the requested board does not exist.
        /// </summary>
        [Fact]
        public async Task Delete_ShouldPropagateKeyNotFoundException_WhenBoardDoesNotExist()
        {
            // Arrange
            var boardId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(Guid.NewGuid());

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () =>
                        _controller.Delete(
                            boardId));

            // Assert
            Assert.Equal(
                "Board not found.",
                exception.Message);

            _boardRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(
                        It.IsAny<Board>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Delete propagates a ForbiddenAccessException
        /// and does not remove the board when the requester is not its owner.
        /// </summary>
        [Fact]
        public async Task Delete_ShouldPropagateForbiddenAccessException_WhenUserIsNotOwner()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var board =
                new Board(
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
                    service.EnsureBoardOwnerAsync(
                        board.Id,
                        userId))
                .ThrowsAsync(
                    new ForbiddenAccessException(
                        "Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () =>
                    _controller.Delete(
                        board.Id));

            // Assert
            _boardRepositoryMock.Verify(
                repository =>
                    repository.RemoveAsync(
                        It.IsAny<Board>()),
                Times.Never);

            _boardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // UPDATE BOARD
        // ============================================================

        /// <summary>
        /// Verifies that Update returns HTTP 204 and updates
        /// the board name when requested by its owner.
        /// </summary>
        [Fact]
        public async Task Update_ShouldReturnNoContent_WhenRequestIsValid()
        {
            // Arrange
            var ownerId = Guid.NewGuid();

            var board =
                new Board(
                    "Original Board",
                    ownerId);

            var request =
                new UpdateBoardRequest
                {
                    Name = "   Updated Board   "
                };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(ownerId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(board.Id))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardOwnerAsync(
                        board.Id,
                        ownerId))
                .Returns(Task.CompletedTask);

            // Act
            var result =
                await _controller.Update(
                    board.Id,
                    request);

            // Assert
            Assert.IsType<NoContentResult>(result);

            Assert.Equal(
                "Updated Board",
                board.Name);

            _boardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Update uses the authenticated user's identifier
        /// when verifying ownership of the board.
        /// </summary>
        [Fact]
        public async Task Update_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var ownerId = Guid.NewGuid();

            var board =
                new Board(
                    "Original",
                    ownerId);

            var request =
                new UpdateBoardRequest
                {
                    Name = "Updated"
                };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(ownerId);

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(board.Id))
                .ReturnsAsync(board);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardOwnerAsync(
                        board.Id,
                        ownerId))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Update(
                board.Id,
                request);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureBoardOwnerAsync(
                        board.Id,
                        ownerId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that Update propagates a KeyNotFoundException
        /// when the requested board does not exist.
        /// </summary>
        [Fact]
        public async Task Update_ShouldPropagateKeyNotFoundException_WhenBoardDoesNotExist()
        {
            // Arrange
            var boardId = Guid.NewGuid();

            var request =
                new UpdateBoardRequest
                {
                    Name = "Updated"
                };

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(Guid.NewGuid());

            _boardRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(boardId))
                .ReturnsAsync((Board?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<KeyNotFoundException>(
                    () =>
                        _controller.Update(
                            boardId,
                            request));

            // Assert
            Assert.Equal(
                "Board not found.",
                exception.Message);

            _boardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        /// <summary>
        /// Verifies that Update propagates a ForbiddenAccessException
        /// and leaves the board unchanged when the requester is not its owner.
        /// </summary>
        [Fact]
        public async Task Update_ShouldPropagateForbiddenAccessException_WhenUserIsNotOwner()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var board =
                new Board(
                    "Original Board",
                    Guid.NewGuid());

            var request =
                new UpdateBoardRequest
                {
                    Name = "Unauthorized Update"
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
                    service.EnsureBoardOwnerAsync(
                        board.Id,
                        userId))
                .ThrowsAsync(
                    new ForbiddenAccessException(
                        "Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () =>
                    _controller.Update(
                        board.Id,
                        request));

            // Assert
            Assert.Equal(
                "Original Board",
                board.Name);

            _boardRepositoryMock.Verify(
                repository =>
                    repository.SaveChangesAsync(),
                Times.Never);
        }

        // ============================================================
        // GET BOARD MEMBERS
        // ============================================================

        /// <summary>
        /// Verifies that GetBoardMembers returns HTTP 200 containing
        /// the users, roles, and profile images associated with the board.
        /// </summary>
        [Fact]
        public async Task GetBoardMembers_ShouldReturnOkWithMembers()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();

            var admin =
                new User(
                    "Admin User",
                    "admin",
                    "admin@example.com",
                    "hash");

            admin.UpdateProfileImage(
                "https://example.com/admin.jpg");

            var member =
                new User(
                    "Member User",
                    "member",
                    "member@example.com",
                    "hash");

            var adminMembership =
                new BoardMember(
                    boardId,
                    admin.Id,
                    BoardRole.Admin);

            var memberMembership =
                new BoardMember(
                    boardId,
                    member.Id,
                    BoardRole.Member);

            AttachUser(
                adminMembership,
                admin);

            AttachUser(
                memberMembership,
                member);

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(requesterId);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardMemberAsync(
                        boardId,
                        requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMembersAsync(
                        boardId))
                .ReturnsAsync(
                    new[]
                    {
                        adminMembership,
                        memberMembership
                    });

            // Act
            var result =
                await _controller.GetBoardMembers(
                    boardId);

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(
                    result.Result);

            Assert.Equal(
                StatusCodes.Status200OK,
                okResult.StatusCode);

            var members =
                Assert.IsAssignableFrom<
                    IEnumerable<BoardMemberResponse>>(
                    okResult.Value)
                    .ToList();

            Assert.Equal(
                2,
                members.Count);

            Assert.Equal(
                admin.Id,
                members[0].UserId);

            Assert.Equal(
                "admin",
                members[0].Username);

            Assert.Equal(
                BoardRole.Admin,
                members[0].Role);

            Assert.Equal(
                "https://example.com/admin.jpg",
                members[0].ProfileImageUrl);

            Assert.Equal(
                member.Id,
                members[1].UserId);

            Assert.Equal(
                "member",
                members[1].Username);

            Assert.Equal(
                BoardRole.Member,
                members[1].Role);
        }

        /// <summary>
        /// Verifies that GetBoardMembers returns an empty collection
        /// when an accessible board currently has no members.
        /// </summary>
        [Fact]
        public async Task GetBoardMembers_ShouldReturnEmptyCollection_WhenBoardHasNoMembers()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(requesterId);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardMemberAsync(
                        boardId,
                        requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMembersAsync(
                        boardId))
                .ReturnsAsync(
                    Array.Empty<BoardMember>());

            // Act
            var result =
                await _controller.GetBoardMembers(
                    boardId);

            // Assert
            var okResult =
                Assert.IsType<OkObjectResult>(
                    result.Result);

            var members =
                Assert.IsAssignableFrom<
                    IEnumerable<BoardMemberResponse>>(
                    okResult.Value);

            Assert.Empty(members);
        }

        /// <summary>
        /// Verifies that GetBoardMembers uses the authenticated user's
        /// identifier when authorizing access to the member list.
        /// </summary>
        [Fact]
        public async Task GetBoardMembers_ShouldUseAuthenticatedUserId()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(requesterId);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardMemberAsync(
                        boardId,
                        requesterId))
                .Returns(Task.CompletedTask);

            _boardMemberRepositoryMock
                .Setup(repository =>
                    repository.GetMembersAsync(
                        boardId))
                .ReturnsAsync(
                    Array.Empty<BoardMember>());

            // Act
            await _controller.GetBoardMembers(
                boardId);

            // Assert
            _currentUserServiceMock.Verify(
                service => service.GetUserId(),
                Times.Once);

            _membershipAuthorizationServiceMock.Verify(
                service =>
                    service.EnsureBoardMemberAsync(
                        boardId,
                        requesterId),
                Times.Once);

            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.GetMembersAsync(
                        boardId),
                Times.Once);
        }

        /// <summary>
        /// Verifies that GetBoardMembers propagates a
        /// ForbiddenAccessException and does not retrieve member data
        /// when board access is denied.
        /// </summary>
        [Fact]
        public async Task GetBoardMembers_ShouldPropagateForbiddenAccessException_WhenAccessIsDenied()
        {
            // Arrange
            var boardId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();

            _currentUserServiceMock
                .Setup(service => service.GetUserId())
                .Returns(requesterId);

            _membershipAuthorizationServiceMock
                .Setup(service =>
                    service.EnsureBoardMemberAsync(
                        boardId,
                        requesterId))
                .ThrowsAsync(
                    new ForbiddenAccessException(
                        "Access denied."));

            // Act
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () =>
                    _controller.GetBoardMembers(
                        boardId));

            // Assert
            _boardMemberRepositoryMock.Verify(
                repository =>
                    repository.GetMembersAsync(
                        It.IsAny<Guid>()),
                Times.Never);
        }

        // ============================================================
        // HELPERS
        // ============================================================

        /// <summary>
        /// Associates a user entity with a board membership for tests
        /// that simulate Entity Framework navigation-property loading.
        /// </summary>
        private static void AttachUser(
            BoardMember membership,
            User user)
        {
            var property =
                typeof(BoardMember)
                    .GetProperty(
                        nameof(BoardMember.User));

            Assert.NotNull(property);

            property!.SetValue(
                membership,
                user);
        }
    }
}