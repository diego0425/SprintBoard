using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintBoard.api.Services;
using SprintBoard.Application.DTOs.Invitation;
using SprintBoard.Application.Services;

namespace SprintBoard.api.Controllers;

/// <summary>
/// Exposes endpoints for responding to board invitations.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/invitations")]
public sealed class InvitationsController : ControllerBase
{
    private readonly InvitationService _invitationService;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvitationsController"/> class.
    /// </summary>
    /// <param name="invitationService">
    /// Application service responsible for accepting and declining board invitations.
    /// </param>
    /// <param name="currentUserService">
    /// Service used to obtain the identifier of the currently authenticated user.
    /// </param>
    public InvitationsController(
        InvitationService invitationService,
        ICurrentUserService currentUserService)
    {
        _invitationService = invitationService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Accepts a pending board invitation for the authenticated user.
    /// </summary>
    /// <param name="request">
    /// Request containing the invitation token to be accepted.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the invitation is accepted successfully.
    /// </returns>
    [HttpPost("accept")]
    public async Task<IActionResult> Accept([FromBody] RespondToInvitationRequest request)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _invitationService.AcceptAsync(request.Token, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Declines a pending board invitation for the authenticated user.
    /// </summary>
    /// <param name="request">
    /// Request containing the invitation token to be declined.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the invitation is declined successfully.
    /// </returns>
    [HttpPost("decline")]
    public async Task<IActionResult> Decline([FromBody] RespondToInvitationRequest request)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _invitationService.DeclineAsync(request.Token, currentUserId);

        return NoContent();
    }
}
