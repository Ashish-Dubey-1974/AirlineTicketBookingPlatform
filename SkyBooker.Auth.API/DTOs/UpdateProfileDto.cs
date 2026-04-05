using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Auth.DTOs;
/// <summary>Request body for PUT /api/auth/profile</summary>
public class UpdateProfileDto
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? PassportNumber { get; set; }

    [MaxLength(60)]
    public string? Nationality { get; set; }

    public string? ProfilePictureUrl { get; set; }
}