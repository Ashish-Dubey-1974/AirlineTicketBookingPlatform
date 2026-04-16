using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Auth.DTOs;

public class GoogleLoginDto
{
    [Required(ErrorMessage = "Google ID token is required")]
    public string IdToken { get; set; } = string.Empty;
}
