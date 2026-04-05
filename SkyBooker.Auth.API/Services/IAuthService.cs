using SkyBooker.Auth.DTOs;
using SkyBooker.Auth.Entities;

namespace SkyBooker.Auth.Services;

/// <summary>
/// Auth service interface — declares all authentication and user management operations.
/// Implemented by AuthService.cs.
/// Registered as Scoped in Program.cs.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user with email/password.
    /// Throws InvalidOperationException if email already exists.
    /// </summary>
    Task<User> Register(RegisterDto dto);

    /// <summary>
    /// Authenticate user with email and password.
    /// Returns AuthResponseDto with JWT token on success.
    /// Returns null if credentials are invalid.
    /// </summary>
    Task<AuthResponseDto?> Login(LoginDto dto);

    /// <summary>
    /// Handle Google OAuth2 callback.
    /// Creates user if first-time login, else returns existing user token.
    /// </summary>
    Task<AuthResponseDto> LoginWithGoogle(string googleIdToken);

    /// <summary>
    /// Validate a JWT token string.
    /// Returns true if token is valid and not expired.
    /// </summary>
    bool ValidateToken(string token);

    /// <summary>
    /// Generate a new JWT token from an existing valid token.
    /// Used to extend session without re-login.
    /// </summary>
    Task<AuthResponseDto?> RefreshToken(string token);

    /// <summary>Get user by UserId. Returns null if not found.</summary>
    Task<User?> GetUserById(int userId);

    /// <summary>Get safe profile DTO (no PasswordHash) by UserId.</summary>
    Task<UserProfileDto?> GetProfile(int userId);

    /// <summary>
    /// Update user profile (FullName, Phone, PassportNumber, Nationality).
    /// Returns updated profile DTO.
    /// </summary>
    Task<UserProfileDto?> UpdateProfile(int userId, UpdateProfileDto dto);

    /// <summary>
    /// Change user password. Verifies old password before updating.
    /// Throws UnauthorizedAccessException if current password is wrong.
    /// </summary>
    Task ChangePassword(int userId, ChangePasswordDto dto);

    /// <summary>
    /// Soft-deactivate a user account (sets IsActive = false).
    /// Does NOT delete data.
    /// </summary>
    Task DeactivateAccount(int userId);

    /// <summary>Get all users. Admin use only.</summary>
    Task<IList<UserProfileDto>> GetAllUsers();

    /// <summary>Map User entity to safe UserProfileDto (no PasswordHash).</summary>
    UserProfileDto MapToProfileDto(User user);
}
