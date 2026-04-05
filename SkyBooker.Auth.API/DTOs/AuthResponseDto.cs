namespace SkyBooker.Auth.DTOs;

/// <summary>Response for successful login/register</summary>
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }
    public UserProfileDto User { get; set; } = new();
}
