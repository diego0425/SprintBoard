using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintBoard.api.Services;
using SprintBoard.Application.DTOs.CardTask;
using SprintBoard.Application.Services;

namespace SprintBoard.api.Controllers;

/// <summary>
/// Exposes endpoints for managing checklist items associated with cards.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1")]
public sealed class CardTasksController : ControllerBase
{
    private readonly CardTaskService _cardTaskService;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CardTasksController"/> class.
    /// </summary>
    /// <param name="cardTaskService">
    /// Application service responsible for creating, retrieving, updating, and removing checklist items.
    /// </param>
    /// <param name="currentUserService">
    /// Service used to obtain the identifier of the currently authenticated user.
    /// </param>
    public CardTasksController(
        CardTaskService cardTaskService,
        ICurrentUserService currentUserService)
    {
        _cardTaskService = cardTaskService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Creates a checklist item in the specified card.
    /// </summary>
    /// <param name="cardId">
    /// Identifier of the card that will receive the new checklist item.
    /// </param>
    /// <param name="request">
    /// Data required to create the checklist item.
    /// </param>
    /// <returns>
    /// A <see cref="CardTaskResponse"/> representing the newly created checklist item.
    /// </returns>
    [HttpPost("cards/{cardId:guid}/tasks")]
    public async Task<ActionResult<CardTaskResponse>> Create(
        Guid cardId,
        [FromBody] CreateCardTaskRequest request)
    {
        var currentUserId = _currentUserService.GetUserId();
        var createdCardTask = await _cardTaskService.CreateAsync(cardId, currentUserId, request);

        return CreatedAtAction(nameof(GetByCard), new { cardId }, createdCardTask);
    }

    /// <summary>
    /// Retrieves all checklist items associated with the specified card.
    /// </summary>
    /// <param name="cardId">
    /// Identifier of the card whose checklist items should be returned.
    /// </param>
    /// <returns>
    /// A collection of <see cref="CardTaskResponse"/> objects associated with the specified card.
    /// </returns>
    [HttpGet("cards/{cardId:guid}/tasks")]
    public async Task<ActionResult<IEnumerable<CardTaskResponse>>> GetByCard(Guid cardId)
    {
        var currentUserId = _currentUserService.GetUserId();
        var cardTasks = await _cardTaskService.GetByCardAsync(cardId, currentUserId);

        return Ok(cardTasks);
    }

    /// <summary>
    /// Marks the specified checklist item as completed.
    /// </summary>
    /// <param name="taskId">
    /// Identifier of the checklist item that should be marked as completed.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the checklist item is updated successfully.
    /// </returns>
    [HttpPatch("cardtasks/{taskId:guid}/complete")]
    public async Task<IActionResult> MarkAsCompleted(Guid taskId)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _cardTaskService.MarkAsCompletedAsync(taskId, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Marks the specified checklist item as pending.
    /// </summary>
    /// <param name="taskId">
    /// Identifier of the checklist item that should be marked as pending.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the checklist item is updated successfully.
    /// </returns>
    [HttpPatch("cardtasks/{taskId:guid}/pending")]
    public async Task<IActionResult> MarkAsPending(Guid taskId)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _cardTaskService.MarkAsPendingAsync(taskId, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Deletes the specified checklist item.
    /// </summary>
    /// <param name="taskId">
    /// Identifier of the checklist item to delete.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the checklist item is deleted successfully.
    /// </returns>
    [HttpDelete("cardtasks/{taskId:guid}")]
    public async Task<IActionResult> Delete(Guid taskId)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _cardTaskService.RemoveAsync(taskId, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Partially updates the specified checklist item.
    /// </summary>
    /// <param name="taskId">
    /// Identifier of the checklist item to update.
    /// </param>
    /// <param name="request">
    /// Checklist item fields that should be updated.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the checklist item is updated successfully.
    /// </returns>
    [HttpPatch("cardtasks/{taskId:guid}")]
    public async Task<IActionResult> Update(
        Guid taskId,
        [FromBody] UpdateCardTaskRequest request)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _cardTaskService.UpdateAsync(taskId, currentUserId, request);

        return NoContent();
    }
}
