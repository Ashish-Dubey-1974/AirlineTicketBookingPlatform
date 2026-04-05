using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SkyBooker.Auth.Entities;

/// <summary>
/// Core user entity for SkyBooker platform.
/// Supports both local (email/password) and OAuth (Google) authentication.
/// </summary>
[Table("users")]
[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Phone))]
[Index(nameof(PassportNumber))]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [EmailAddress]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(256)]
    [Column("password_hash")]
    public string? PasswordHash { get; set; }  // nullable for OAuth users

    [MaxLength(20)]
    [Phone]
    [Column("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Role: PASSENGER | AIRLINE_STAFF | ADMIN
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("role")]
    public string Role { get; set; } = UserRoles.Passenger;

    /// <summary>
    /// Provider: local | google
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("provider")]
    public string Provider { get; set; } = AuthProviders.Local;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [MaxLength(20)]
    [Column("passport_number")]
    public string? PassportNumber { get; set; }

    [MaxLength(60)]
    [Column("nationality")]
    public string? Nationality { get; set; }

    [Column("profile_picture_url")]
    public string? ProfilePictureUrl { get; set; }

    [Column("google_id")]
    public string? GoogleId { get; set; }  // stores Google sub claim

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }
}


