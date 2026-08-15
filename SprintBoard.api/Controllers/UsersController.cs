using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintBoard.api.Services;
using SprintBoard.Application.DTOs.User;
using SprintBoard.Application.Services;

namespace SprintBoard.api.Controllers;

/// <summary>
/// Exposes endpoints for retrieving and updating users and profile images.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    private static readonly string[] AllowedProfileImageContentTypes =
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly UserService _userService;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    /// <param name="userService">
    /// Application service responsible for retrieving and updating user data.
    /// </param>
    /// <param name="currentUserService">
    /// Service used to obtain the identifier of the currently authenticated user.
    /// </param>
    public UsersController(
        UserService userService,
        ICurrentUserService currentUserService)
    {
        _userService = userService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Retrieves the profile of the currently authenticated user.
    /// </summary>
    /// <returns>
    /// An object containing the authenticated user's profile information.
    /// </returns>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var currentUserId = _currentUserService.GetUserId();
        var currentUser = await _userService.GetByIdAsync(currentUserId);

        if (currentUser is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            currentUser.Id,
            currentUser.Username,
            currentUser.FullName,
            currentUser.Email,
            currentUser.ProfileImageUrl
        });
    }

    /// <summary>
    /// Replaces the authenticated user's profile image.
    /// </summary>
    /// <param name="file">
    /// Image file to upload as the new profile image. Only JPG, PNG, and WebP files are supported.
    /// </param>
    /// <returns>
    /// An object containing the URL of the uploaded profile image.
    /// </returns>
    [HttpPatch("me/profile-image")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UpdateProfileImage(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("File is required.");
        }

        if (!AllowedProfileImageContentTypes.Contains(file.ContentType))
        {
            return BadRequest("Only JPG, PNG and WEBP images are allowed.");
        }

        var currentUserId = _currentUserService.GetUserId();

        await using var fileStream = file.OpenReadStream();

        var profileImageUrl = await _userService.UpdateProfileImageAsync(
            currentUserId,
            fileStream,
            file.FileName,
            file.ContentType);

        return Ok(new { profileImageUrl });
    }

    /// <summary>
    /// Partially updates the authenticated user's profile.
    /// </summary>
    /// <param name="request">
    /// Profile fields that should be updated for the authenticated user.
    /// </param>
    /// <returns>
    /// A <see cref="NoContentResult"/> when the profile is updated successfully.
    /// </returns>
    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequest request)
    {
        var currentUserId = _currentUserService.GetUserId();

        await _userService.UpdateMeAsync(currentUserId, request);

        return NoContent();
    }
}
