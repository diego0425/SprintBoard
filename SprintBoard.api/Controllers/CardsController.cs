using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintBoard.api.Services;
using SprintBoard.Application.DTOs.Card;
using SprintBoard.Application.Services;

namespace SprintBoard.api.Controllers;

/// <summary>
/// Exposes endpoints for updating and deleting cards.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/cards")]
public sealed class CardsController : ControllerBase
{
    private readonly CardService _cardService;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CardsController"/> class.
    /// </summary>
    /// <param name="cardService">
    /// Application service responsible for updating card data and removing cards.
    /// </param>
    /// <param name="currentUserService">
    /// Service used to obtain the identifier of the currently authenticated user.
    /// </param>
    public CardsController(
        CardService cardService,
        ICurrentUserService currentUserService)
    {
        _cardService = cardService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Changes the status of the specified card.
    /// </summary>
    /// <param name="cardId">
    /// Identifier of the card whose status will be changed.
    /// </param>
    /// <param name="request">
    /// Request data containing the new card status.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the card status is updated successfully.
    /// </returns>
    [HttpPatch("{cardId:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid cardId,
        [FromBody] UpdateCardStatusRequest request)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _cardService.ChangeStatusAsync(cardId, currentUserId, request.Status);

        return NoContent();
    }

    /// <summary>
    /// Deletes the specified card.
    /// </summary>
    /// <param name="cardId">
    /// Identifier of the card to delete.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the card is deleted successfully.
    /// </returns>
    [HttpDelete("{cardId:guid}")]
    public async Task<IActionResult> Delete(Guid cardId)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _cardService.RemoveAsync(cardId, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Partially updates the specified card.
    /// </summary>
    /// <param name="cardId">
    /// Identifier of the card to update.
    /// </param>
    /// <param name="request">
    /// Card fields that should be updated.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the card is updated successfully.
    /// </returns>
    [HttpPatch("{cardId:guid}")]
    public async Task<IActionResult> Update(
        Guid cardId,
        [FromBody] UpdateCardRequest request)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _cardService.UpdateAsync(cardId, currentUserId, request);

        return NoContent();
    }
}
