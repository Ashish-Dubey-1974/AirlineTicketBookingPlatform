using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SkyBooker.Passenger.API.Entities;

[Table("passenger_info")]
[Index(nameof(BookingId))]
[Index(nameof(PassportNumber))]
[Index(nameof(TicketNumber))]
[Index(nameof(SeatId))]
public class PassengerInfo
{
    [Key]
    [Column("passenger_id")]
    public int PassengerId { get; set; }

    [Required]
    [MaxLength(36)]
    [Column("booking_id")]
    public string BookingId { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Column("date_of_birth")]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("gender")]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("passport_number")]
    public string PassportNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    [Column("nationality")]
    public string Nationality { get; set; } = string.Empty;

    [Required]
    [Column("passport_expiry")]
    public DateTime PassportExpiry { get; set; }

    [Column("seat_id")]
    public int? SeatId { get; set; }

    [MaxLength(5)]
    [Column("seat_number")]
    public string? SeatNumber { get; set; }

    [MaxLength(50)]
    [Column("ticket_number")]
    public string? TicketNumber { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("passenger_type")]
    public string PassengerType { get; set; } = "ADULT";

    [Column("checked_in")]
    public bool CheckedIn { get; set; }

    [Column("checked_in_at")]
    public DateTime? CheckedInAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public static class PassengerTypeConstants
{
    public const string Adult = "ADULT";
    public const string Child = "CHILD";
    public const string Infant = "INFANT";
}