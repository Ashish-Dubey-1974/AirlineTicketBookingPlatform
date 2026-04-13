using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Passenger.API.DTOs;

public class PassengerRequestDto
{
    [Required]
    [MaxLength(36)]
    public string BookingId { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [MaxLength(10)]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PassportNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string Nationality { get; set; } = string.Empty;

    [Required]
    public DateTime PassportExpiry { get; set; }

    public int? SeatId { get; set; }

    [MaxLength(5)]
    public string? SeatNumber { get; set; }

    [MaxLength(20)]
    public string PassengerType { get; set; } = "ADULT";
}

public class AssignSeatDto
{
    [Required]
    public int PassengerId { get; set; }

    [Required]
    public int SeatId { get; set; }

    [Required]
    [MaxLength(5)]
    public string SeatNumber { get; set; } = string.Empty;
}

public class WebCheckInDto
{
    [Required]
    public int PassengerId { get; set; }

    [Required]
    public int NewSeatId { get; set; }

    [Required]
    [MaxLength(5)]
    public string NewSeatNumber { get; set; } = string.Empty;
}