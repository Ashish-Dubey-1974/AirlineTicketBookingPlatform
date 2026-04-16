using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Auth.DTOs;

public class AssignRoleDto
{
    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = string.Empty;
}
