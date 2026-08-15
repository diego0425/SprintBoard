using Microsoft.AspNetCore.Mvc;
using SprintBoard.api.Auth;
using SprintBoard.Application.DTOs.Auth;
using SprintBoard.Application.Services;

namespace SprintBoard.api.Controllers;

/// <summary>
/// Exposes endpoints for user registration and authentication.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly JwtTokenService _jwtTokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="authService">
    /// Application service responsible for registering users and validating login credentials.
    /// </param>
    /// <param name="jwtTokenService">
    /// Service responsible for generating JWT access tokens after a successful authentication operation.
    /// </param>
    public AuthController(AuthService authService, JwtTokenService jwtTokenService)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Registers a new user and returns an access token for the created account.
    /// </summary>
    /// <param name="request">
    /// Registration data used to create the user account, such as name, email, username, and password.
    /// </param>
    /// <returns>
    /// An <see cref="AuthResponse"/> containing the generated JWT access token and its expiration date.
    /// </returns>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var user = await _authService.RegisterAsync(request);
        var (accessToken, expiresAtUtc) = _jwtTokenService.CreateToken(user);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAtUtc = expiresAtUtc
        });
    }

    /// <summary>
    /// Authenticates an existing user and returns a new access token.
    /// </summary>
    /// <param name="request">
    /// User credentials used to validate the login attempt.
    /// </param>
    /// <returns>
    /// An <see cref="AuthResponse"/> containing the generated JWT access token and its expiration date.
    /// </returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.LoginAsync(request);
        var (accessToken, expiresAtUtc) = _jwtTokenService.CreateToken(user);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAtUtc = expiresAtUtc
        });
    }
}
