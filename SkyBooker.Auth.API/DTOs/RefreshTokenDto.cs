using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Auth.DTOs;

/// <summary>Request body for POST /api/auth/refresh</summary>
public class RefreshTokenDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}