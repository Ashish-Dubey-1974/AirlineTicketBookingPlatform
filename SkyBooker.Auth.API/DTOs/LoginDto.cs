using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Auth.DTOs;
/// <summary>Request body for POST /api/auth/login</summary>
public class LoginDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}