using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintBoard.api.Services;
using SprintBoard.Application.DTOs.Board;
using SprintBoard.Application.DTOs.Card;
using SprintBoard.Application.Services;

namespace SprintBoard.api.Controllers;

/// <summary>
/// Exposes endpoints for board management, membership administration, cards, and invitations.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/boards")]
public sealed class BoardsController : ControllerBase
{
    private readonly BoardService _boardService;
    private readonly CardService _cardService;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoardsController"/> class.
    /// </summary>
    /// <param name="boardService">
    /// Application service responsible for creating, retrieving, updating, deleting, and managing boards.
    /// </param>
    /// <param name="cardService">
    /// Application service responsible for card operations performed within boards.
    /// </param>
    /// <param name="currentUserService">
    /// Service used to obtain the identifier of the currently authenticated user.
    /// </param>
    public BoardsController(
        BoardService boardService,
        CardService cardService,
        ICurrentUserService currentUserService)
    {
        _boardService = boardService;
        _cardService = cardService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Creates a board owned by the authenticated user.
    /// </summary>
    /// <param name="request">
    /// Data required to create the board, including its name.
    /// </param>
    /// <returns>
    /// A <see cref="BoardResponse"/> representing the newly created board.
    /// </returns>
    [HttpPost]
    public async Task<ActionResult<BoardResponse>> Create([FromBody] CreateBoardRequest request)
    {
        var currentUserId = _currentUserService.GetUserId();
        var createdBoard = await _boardService.CreateAsync(request.Name, currentUserId);

        return CreatedAtAction(nameof(GetById), new { id = createdBoard.Id }, createdBoard);
    }

    /// <summary>
    /// Creates a new card within the specified board.
    /// </summary>
    /// <param name="boardId">
    /// Identifier of the board that will receive the new card.
    /// </param>
    /// <param name="request">
    /// Data required to create the card, such as title, description, and status.
    /// </param>
    /// <returns>
    /// A <see cref="CardResponse"/> describing the newly created card.
    /// </returns>
    [HttpPost("{boardId:guid}/cards")]
    public async Task<ActionResult<CardResponse>> CreateCard(
        Guid boardId,
        [FromBody] CreateCardRequest request)
    {
        var currentUserId = _currentUserService.GetUserId();
        var createdCard = await _cardService.CreateAsync(boardId, currentUserId, request);

        return Created($"/api/v1/boards/{boardId}/cards/{createdCard.Id}", createdCard);
    }

    /// <summary>
    /// Retrieves all cards that belong to the specified board.
    /// </summary>
    /// <param name="boardId">
    /// Identifier of the board whose cards should be retrieved.
    /// </param>
    /// <returns>
    /// A collection of <see cref="CardResponse"/> instances associated with the board.
    /// </returns>
    [HttpGet("{boardId:guid}/cards")]
    public async Task<ActionResult<IEnumerable<CardResponse>>> GetCards(Guid boardId)
    {
        var currentUserId = _currentUserService.GetUserId();
        var cards = await _cardService.GetByBoardAsync(boardId, currentUserId);

        return Ok(cards);
    }

    /// <summary>
    /// Retrieves a board by its identifier.
    /// </summary>
    /// <param name="id">
    /// Identifier of the board to retrieve.
    /// </param>
    /// <returns>
    /// A <see cref="BoardResponse"/> containing the requested board data.
    /// </returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BoardResponse>> GetById(Guid id)
    {
        var currentUserId = _currentUserService.GetUserId();
        var board = await _boardService.GetByIdAsync(id, currentUserId);

        return Ok(board);
    }

    /// <summary>
    /// Creates and sends an invitation for a user to join the specified board.
    /// Owners and administrators are allowed to perform this operation.
    /// </summary>
    /// <param name="boardId">
    /// Identifier of the board for which the invitation will be created.
    /// </param>
    /// <param name="request">
    /// Invitation data containing the email address of the user to invite.
    /// </param>
    /// <returns>
    /// A <see cref="BoardInvitationResponse"/> representing the created invitation.
    /// </returns>
    [HttpPost("{boardId:guid}/invitations")]
    public async Task<ActionResult<BoardInvitationResponse>> CreateInvitation(
        Guid boardId,
        [FromBody] CreateBoardInvitationRequest request)
    {
        var currentUserId = _currentUserService.GetUserId();
        var invitation = await _boardService.CreateInvitationAsync(
            boardId,
            currentUserId,
            request.Email);

        return Ok(invitation);
    }

    /// <summary>
    /// Changes the role of a member within the specified board.
    /// </summary>
    /// <param name="boardId">
    /// Identifier of the board that contains the member whose role will be updated.
    /// </param>
    /// <param name="request">
    /// Data identifying the member and the new role to assign.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the member role is updated successfully.
    /// </returns>
    [HttpPatch("{boardId:guid}/members/role")]
    public async Task<IActionResult> ChangeRole(
        Guid boardId,
        [FromBody] ChangeBoardMemberRoleRequest request)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _boardService.ChangeMemberRoleAsync(
            boardId,
            currentUserId,
            request.MemberUserId,
            request.NewRole);

        return NoContent();
    }

    /// <summary>
    /// Removes a member from the specified board.
    /// </summary>
    /// <param name="boardId">
    /// Identifier of the board from which the member will be removed.
    /// </param>
    /// <param name="memberUserId">
    /// Identifier of the member to remove.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the member is removed successfully.
    /// </returns>
    [HttpDelete("{boardId:guid}/members/{memberUserId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid boardId, Guid memberUserId)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _boardService.RemoveMemberAsync(
            boardId,
            currentUserId,
            memberUserId);

        return NoContent();
    }

    /// <summary>
    /// Removes the authenticated user's own membership from the specified board.
    /// </summary>
    /// <param name="boardId">
    /// Identifier of the board that the authenticated user wants to leave.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the user leaves the board successfully.
    /// </returns>
    [HttpDelete("{boardId:guid}/members/me")]
    public async Task<IActionResult> LeaveBoard(Guid boardId)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _boardService.LeaveBoardAsync(boardId, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Retrieves all boards in which the authenticated user is either the owner or a member.
    /// </summary>
    /// <returns>
    /// A collection of <see cref="BoardResponse"/> objects accessible to the authenticated user.
    /// </returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BoardResponse>>> GetMyBoards()
    {
        var currentUserId = _currentUserService.GetUserId();
        var boards = await _boardService.GetByUserAsync(currentUserId);

        return Ok(boards);
    }

    /// <summary>
    /// Deletes the specified board.
    /// </summary>
    /// <param name="boardId">
    /// Identifier of the board to delete.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the board is deleted successfully.
    /// </returns>
    [HttpDelete("{boardId:guid}")]
    public async Task<IActionResult> Delete(Guid boardId)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _boardService.RemoveAsync(boardId, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Partially updates the specified board.
    /// </summary>
    /// <param name="boardId">
    /// Identifier of the board to update.
    /// </param>
    /// <param name="request">
    /// Board fields that should be updated.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the board is updated successfully.
    /// </returns>
    [HttpPatch("{boardId:guid}")]
    public async Task<IActionResult> Update(
        Guid boardId,
        [FromBody] UpdateBoardRequest request)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _boardService.UpdateAsync(boardId, currentUserId, request);

        return NoContent();
    }

    /// <summary>
    /// Retrieves all members associated with the specified board.
    /// </summary>
    /// <param name="boardId">
    /// Identifier of the board whose members should be returned.
    /// </param>
    /// <returns>
    /// A collection of <see cref="BoardMemberResponse"/> objects representing the board members.
    /// </returns>
    [HttpGet("{boardId:guid}/members")]
    public async Task<ActionResult<IEnumerable<BoardMemberResponse>>> GetBoardMembers(Guid boardId)
    {
        var currentUserId = _currentUserService.GetUserId();
        var boardMembers = await _boardService.GetBoardMembersAsync(boardId, currentUserId);

        return Ok(boardMembers);
    }
}
