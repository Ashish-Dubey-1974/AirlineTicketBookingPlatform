using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SkyBooker.Bookings.API.Entities;

[Table("bookings")]
[Index(nameof(PnrCode), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(FlightId))]
[Index(nameof(Status))]
[Index(nameof(BookedAt))]
public class Booking
{
    [Key]
    [MaxLength(36)]
    [Column("booking_id")]
    public string BookingId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("flight_id")]
    public int FlightId { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("pnr_code")]
    public string PnrCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("trip_type")]
    public string TripType { get; set; } = TripTypeConstants.OneWay;

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = BookingStatusConstants.Pending;

    [Column("total_fare", TypeName = "decimal(18,2)")]
    public decimal TotalFare { get; set; }

    [Column("base_fare", TypeName = "decimal(18,2)")]
    public decimal BaseFare { get; set; }

    [Column("taxes", TypeName = "decimal(18,2)")]
    public decimal Taxes { get; set; }

    [Column("ancillary_charges", TypeName = "decimal(18,2)")]
    public decimal AncillaryCharges { get; set; }

    [MaxLength(20)]
    [Column("meal_preference")]
    public string? MealPreference { get; set; }

    [Column("luggage_kg")]
    public int LuggageKg { get; set; } = 15;

    [MaxLength(150)]
    [EmailAddress]
    [Column("contact_email")]
    public string ContactEmail { get; set; } = string.Empty;

    [MaxLength(20)]
    [Phone]
    [Column("contact_phone")]
    public string ContactPhone { get; set; } = string.Empty;

    [Column("booked_at")]
    public DateTime BookedAt { get; set; } = DateTime.UtcNow;

    [Column("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }

    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    [MaxLength(36)]
    [Column("payment_id")]
    public string? PaymentId { get; set; }

    [MaxLength(500)]
    [Column("cancellation_reason")]
    public string? CancellationReason { get; set; }

    [Column("refund_amount", TypeName = "decimal(18,2)")]
    public decimal? RefundAmount { get; set; }

    [Column("return_flight_id")]
    public int? ReturnFlightId { get; set; }
}