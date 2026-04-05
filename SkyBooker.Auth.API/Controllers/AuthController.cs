using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.Auth.DTOs;
using SkyBooker.Auth.Entities;
using SkyBooker.Auth.Services;

namespace SkyBooker.Auth.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger      = logger;
    }

    // ── POST /api/auth/register ───────────────────────────────────────────────
    /// <summary>Register a new user (Passenger or Airline Staff)</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponseDto
            {
                Message    = "Validation failed",
                Details    = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)),
                StatusCode = 400
            });

        try
        {
            var user    = await _authService.Register(dto);
            var profile = _authService.MapToProfileDto(user);
            _logger.LogInformation("New user registered: {Email}", dto.Email);
            return CreatedAtAction(nameof(GetProfile), new { }, profile);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponseDto
            {
                Message    = ex.Message,
                StatusCode = 409
            });
        }
    }

    // ── POST /api/auth/login ──────────────────────────────────────────────────
    /// <summary>Login with email and password — returns JWT token</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.Login(dto);
        if (result == null)
            return Unauthorized(new ErrorResponseDto
            {
                Message    = "Invalid email or password.",
                StatusCode = 401
            });

        return Ok(result);
    }

    // ── POST /api/auth/google ─────────────────────────────────────────────────
    /// <summary>Login/Register via Google OAuth2 — pass the Google ID token</summary>
    [HttpPost("google")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LoginWithGoogle([FromBody] string googleIdToken)
    {
        try
        {
            var result = await _authService.LoginWithGoogle(googleIdToken);
            return Ok(result);
        }
        catch (NotImplementedException ex)
        {
            // Will be implemented in Day 2
            return StatusCode(501, new ErrorResponseDto { Message = ex.Message, StatusCode = 501 });
        }
    }

    // ── POST /api/auth/logout ─────────────────────────────────────────────────
    /// <summary>Logout — client should discard the JWT token</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        // JWT is stateless — actual logout is handled client-side by deleting the token.
        // In production, maintain a token blacklist in Redis for forced invalidation.
        var userId = User.FindFirst("userId")?.Value;
        _logger.LogInformation("User logged out: UserId={UserId}", userId);
        return Ok(new { message = "Logged out successfully. Please discard your token." });
    }

    // ── POST /api/auth/refresh ────────────────────────────────────────────────
    /// <summary>Refresh a JWT token before it expires</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var result = await _authService.RefreshToken(dto.Token);
        if (result == null)
            return Unauthorized(new ErrorResponseDto { Message = "Invalid or expired token.", StatusCode = 401 });

        return Ok(result);
    }

    // ── GET /api/auth/profile ─────────────────────────────────────────────────
    /// <summary>Get current authenticated user's profile</summary>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var profile = await _authService.GetProfile(userId.Value);
        if (profile == null) return NotFound();

        return Ok(profile);
    }

    // ── PUT /api/auth/profile ─────────────────────────────────────────────────
    /// <summary>Update authenticated user's profile</summary>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var updated = await _authService.UpdateProfile(userId.Value, dto);
        if (updated == null) return NotFound();

        return Ok(updated);
    }

    // ── PUT /api/auth/password ────────────────────────────────────────────────
    /// <summary>Change authenticated user's password</summary>
    [HttpPut("password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _authService.ChangePassword(userId.Value, dto);
            return Ok(new { message = "Password changed successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ErrorResponseDto { Message = ex.Message, StatusCode = 401 });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message, StatusCode = 400 });
        }
    }

    // ── DELETE /api/auth/deactivate/{id} ──────────────────────────────────────
    /// <summary>Deactivate a user account (soft delete) — Admin only</summary>
    [HttpDelete("deactivate/{id:int}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateAccount(int id)
    {
        try
        {
            await _authService.DeactivateAccount(id);
            return Ok(new { message = $"User {id} deactivated successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponseDto { Message = ex.Message, StatusCode = 404 });
        }
    }

    // ── GET /api/auth/users ───────────────────────────────────────────────────
    /// <summary>Get all users — Admin only</summary>
    [HttpGet("users")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(IList<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _authService.GetAllUsers();
        return Ok(users);
    }

    // ── PUT /api/auth/users/{id}/role ─────────────────────────────────────────
    /// <summary>Assign a role to a user — Admin only</summary>
    [HttpPut("users/{id:int}/role")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(int id, [FromBody] string role)
    {
        try
        {
            var updated = await _authService.AssignRole(id, role);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message, StatusCode = 400 });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponseDto { Message = ex.Message, StatusCode = 404 });
        }
    }

    // ── GET /api/auth/users/{id} ──────────────────────────────────────────────
    /// <summary>Get a specific user by ID — Admin only</summary>
    [HttpGet("users/{id:int}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(int id)
    {
        var profile = await _authService.GetProfile(id);
        if (profile == null)
            return NotFound(new ErrorResponseDto { Message = $"User {id} not found.", StatusCode = 404 });

        return Ok(profile);
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────────────────────
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("userId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}
