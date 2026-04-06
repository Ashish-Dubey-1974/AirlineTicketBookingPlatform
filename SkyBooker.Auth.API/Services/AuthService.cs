using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SkyBooker.Auth.DTOs;
using SkyBooker.Auth.Entities;
using SkyBooker.Auth.Repositories;

namespace SkyBooker.Auth.Services;

/// <summary>
/// Full implementation of IAuthService.
/// Day 2 update: LoginWithGoogle now fully implemented using Google.Apis.Auth.
/// Uses PasswordHasher[User] from ASP.NET Core Identity for PBKDF2+HMAC-SHA256 hashing.
/// Uses JwtSecurityTokenHandler for JWT generation and validation.
/// Injected via constructor — registered as Scoped in Program.cs.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    // JWT config — loaded once from appsettings.json
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpiryHours;

    // Google OAuth config
    private readonly string? _googleClientId;

    public AuthService(
        IUserRepository userRepo,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _userRepo       = userRepo;
        _config         = config;
        _logger         = logger;
        _passwordHasher = new PasswordHasher<User>();

        // Read JWT settings from appsettings.json
        _jwtSecret      = config["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret not configured");
        _jwtIssuer      = config["JwtSettings:Issuer"] ?? "SkyBooker.Auth.API";
        _jwtAudience    = config["JwtSettings:Audience"] ?? "SkyBooker.Clients";
        _jwtExpiryHours = int.Parse(config["JwtSettings:ExpiryHours"] ?? "24");

        // Google OAuth Client ID for token validation
        _googleClientId = config["Google:ClientId"];
    }

    // ── REGISTER ──────────────────────────────────────────────────────────────
    public async Task<User> Register(RegisterDto dto)
    {
        if (await _userRepo.ExistsByEmail(dto.Email))
        {
            _logger.LogWarning("Registration failed: email already exists — {Email}", dto.Email);
            throw new InvalidOperationException($"User with email '{dto.Email}' already exists.");
        }

        var user = new User
        {
            FullName    = dto.FullName.Trim(),
            Email       = dto.Email.Trim().ToLower(),
            Phone       = dto.Phone?.Trim(),
            Provider    = AuthProviders.Local,
            Nationality = dto.Nationality,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        var saved = await _userRepo.Save(user);
        _logger.LogInformation("User registered: UserId={UserId}, Email={Email}", saved.UserId, saved.Email);
        return saved;
    }

    // ── LOGIN ─────────────────────────────────────────────────────────────────
    public async Task<AuthResponseDto?> Login(LoginDto dto)
    {
        var user = await _userRepo.FindByEmail(dto.Email.Trim().ToLower());
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Login failed: user not found or inactive — {Email}", dto.Email);
            return null;
        }

        if (user.PasswordHash == null)
        {
            _logger.LogWarning("Login failed: OAuth user trying password login — {Email}", dto.Email);
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed: wrong password — {Email}", dto.Email);
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepo.Update(user);

        var token  = GenerateJwtToken(user);
        var expiry = DateTime.UtcNow.AddHours(_jwtExpiryHours);

        _logger.LogInformation("User logged in: UserId={UserId}, Email={Email}", user.UserId, user.Email);

        return new AuthResponseDto
        {
            Token     = token,
            TokenType = "Bearer",
            ExpiresAt = expiry,
            User      = MapToProfileDto(user)
        };
    }

    // ── GOOGLE OAUTH — DAY 2 COMPLETE IMPLEMENTATION ──────────────────────────
    /// <summary>
    /// Validates a Google ID token using Google.Apis.Auth.
    /// If user exists → login. If new → auto-register as PASSENGER.
    /// Never stores a password hash for OAuth users (PasswordHash remains null).
    /// </summary>
    public async Task<AuthResponseDto> LoginWithGoogle(string googleIdToken)
    {
        if (string.IsNullOrWhiteSpace(_googleClientId))
            throw new InvalidOperationException(
                "Google:ClientId is not configured in appsettings.json.");

        // 1. Validate the Google ID token with Google's public keys
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleClientId }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(googleIdToken, validationSettings);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Google OAuth: invalid ID token — {Message}", ex.Message);
            throw new UnauthorizedAccessException("Invalid Google ID token.");
        }

        var email    = payload.Email.ToLower();
        var googleId = payload.Subject; // Google's unique user sub claim
        var fullName = payload.Name ?? payload.Email;
        var picture  = payload.Picture;

        _logger.LogInformation("Google OAuth: validated token for {Email} (sub={GoogleId})", email, googleId);

        // 2. Try to find existing user by Google sub claim first, then by email
        var user = await _userRepo.FindByGoogleId(googleId)
                ?? await _userRepo.FindByEmail(email);

        if (user == null)
        {
            // 3a. New user — auto-register
            user = new User
            {
                FullName          = fullName,
                Email             = email,
                GoogleId          = googleId,
                ProfilePictureUrl = picture,
                Role              = UserRoles.Passenger,
                Provider          = AuthProviders.Google,
                IsActive          = true,
                PasswordHash      = null, // OAuth users have no password
                CreatedAt         = DateTime.UtcNow,
                UpdatedAt         = DateTime.UtcNow,
                LastLoginAt       = DateTime.UtcNow
            };

            user = await _userRepo.Save(user);
            _logger.LogInformation("Google OAuth: new user auto-registered — UserId={UserId}, Email={Email}",
                user.UserId, user.Email);
        }
        else
        {
            // 3b. Existing user — update Google ID and last login if needed
            if (user.GoogleId == null)
            {
                user.GoogleId  = googleId;
                user.Provider  = AuthProviders.Google;
            }
            if (picture != null)
                user.ProfilePictureUrl = picture;

            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt   = DateTime.UtcNow;
            await _userRepo.Update(user);

            _logger.LogInformation("Google OAuth: existing user login — UserId={UserId}, Email={Email}",
                user.UserId, user.Email);
        }

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Your account has been deactivated. Please contact support.");

        // 4. Generate JWT
        var jwtToken = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token     = jwtToken,
            TokenType = "Bearer",
            ExpiresAt = DateTime.UtcNow.AddHours(_jwtExpiryHours),
            User      = MapToProfileDto(user)
        };
    }

    // ── VALIDATE TOKEN ────────────────────────────────────────────────────────
    public bool ValidateToken(string token)
    {
        try
        {
            var handler    = new JwtSecurityTokenHandler();
            var key        = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = key,
                ValidateIssuer           = true,
                ValidIssuer              = _jwtIssuer,
                ValidateAudience         = true,
                ValidAudience            = _jwtAudience,
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero
            };

            handler.ValidateToken(token, parameters, out _);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Token validation failed: {Message}", ex.Message);
            return false;
        }
    }

    // ── REFRESH TOKEN ─────────────────────────────────────────────────────────
    public async Task<AuthResponseDto?> RefreshToken(string token)
    {
        try
        {
            var handler    = new JwtSecurityTokenHandler();
            var key        = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = key,
                ValidateIssuer           = true,
                ValidIssuer              = _jwtIssuer,
                ValidateAudience         = true,
                ValidAudience            = _jwtAudience,
                ValidateLifetime         = false // allow expired for refresh
            };

            var principal   = handler.ValidateToken(token, parameters, out _);
            var userIdClaim = principal.FindFirst("userId")?.Value;

            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return null;

            var user = await _userRepo.FindByUserId(userId);
            if (user == null || !user.IsActive) return null;

            var newToken = GenerateJwtToken(user);
            return new AuthResponseDto
            {
                Token     = newToken,
                TokenType = "Bearer",
                ExpiresAt = DateTime.UtcNow.AddHours(_jwtExpiryHours),
                User      = MapToProfileDto(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Token refresh failed: {Message}", ex.Message);
            return null;
        }
    }

    // ── GET USER ──────────────────────────────────────────────────────────────
    public async Task<User?> GetUserById(int userId)
        => await _userRepo.FindByUserId(userId);

    public async Task<UserProfileDto?> GetProfile(int userId)
    {
        var user = await _userRepo.FindByUserId(userId);
        return user == null ? null : MapToProfileDto(user);
    }

    // ── UPDATE PROFILE ────────────────────────────────────────────────────────
    public async Task<UserProfileDto?> UpdateProfile(int userId, UpdateProfileDto dto)
    {
        var user = await _userRepo.FindByUserId(userId);
        if (user == null) return null;

        user.FullName          = dto.FullName.Trim();
        user.Phone             = dto.Phone?.Trim();
        user.PassportNumber    = dto.PassportNumber?.Trim().ToUpper();
        user.Nationality       = dto.Nationality?.Trim();
        user.ProfilePictureUrl = dto.ProfilePictureUrl;
        user.UpdatedAt         = DateTime.UtcNow;

        var updated = await _userRepo.Update(user);
        _logger.LogInformation("Profile updated: UserId={UserId}", userId);
        return MapToProfileDto(updated);
    }

    // ── CHANGE PASSWORD ───────────────────────────────────────────────────────
    public async Task ChangePassword(int userId, ChangePasswordDto dto)
    {
        var user = await _userRepo.FindByUserId(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found");

        if (user.PasswordHash == null)
            throw new InvalidOperationException("OAuth users cannot change password via this endpoint.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        user.UpdatedAt    = DateTime.UtcNow;
        await _userRepo.Update(user);

        _logger.LogInformation("Password changed: UserId={UserId}", userId);
    }

    // ── DEACTIVATE ────────────────────────────────────────────────────────────
    public async Task DeactivateAccount(int userId)
    {
        var user = await _userRepo.FindByUserId(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found");

        user.IsActive  = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.Update(user);

        _logger.LogWarning("Account deactivated: UserId={UserId}", userId);
    }

    // ── GET ALL USERS ─────────────────────────────────────────────────────────
    public async Task<IList<UserProfileDto>> GetAllUsers()
    {
        var users = await _userRepo.FindAll();
        return users.Select(MapToProfileDto).ToList();
    }

    // ── ASSIGN ROLE ───────────────────────────────────────────────────────────
    public async Task<UserProfileDto?> AssignRole(int userId, string role)
    {
        var validRoles = new[] { UserRoles.Passenger, UserRoles.AirlineStaff, UserRoles.Admin };
        if (!validRoles.Contains(role))
            throw new ArgumentException($"Invalid role '{role}'. Valid: {string.Join(", ", validRoles)}");

        var user = await _userRepo.FindByUserId(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found");

        user.Role      = role;
        user.UpdatedAt = DateTime.UtcNow;
        var updated = await _userRepo.Update(user);

        _logger.LogWarning("Role assigned: UserId={UserId} → Role={Role}", userId, role);
        return MapToProfileDto(updated);
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────
    private string GenerateJwtToken(User user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("userId",             user.UserId.ToString()),
            new Claim(ClaimTypes.Email,     user.Email),
            new Claim(ClaimTypes.Role,      user.Role),
            new Claim(ClaimTypes.Name,      user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer:             _jwtIssuer,
            audience:           _jwtAudience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddHours(_jwtExpiryHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public UserProfileDto MapToProfileDto(User user) => new UserProfileDto
    {
        UserId            = user.UserId,
        FullName          = user.FullName,
        Email             = user.Email,
        Phone             = user.Phone,
        Role              = user.Role,
        Provider          = user.Provider,
        IsActive          = user.IsActive,
        PassportNumber    = user.PassportNumber,
        Nationality       = user.Nationality,
        ProfilePictureUrl = user.ProfilePictureUrl,
        CreatedAt         = user.CreatedAt,
        LastLoginAt       = user.LastLoginAt
    };
}
