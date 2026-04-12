using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SkyBooker.Payment.API.Entities;

[Table("payments")]
[Index(nameof(BookingId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(TransactionId))]
[Index(nameof(Status))]
public class Payments
{
    [Key]
    [MaxLength(36)]
    [Column("payment_id")]
    public string PaymentId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(36)]
    [Column("booking_id")]
    public string BookingId { get; set; } = string.Empty;

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(3)]
    [Column("currency")]
    public string Currency { get; set; } = "INR";

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "PENDING";

    [Required]
    [MaxLength(20)]
    [Column("payment_mode")]
    public string PaymentMode { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("transaction_id")]
    public string? TransactionId { get; set; }

    [Column("gateway_response", TypeName = "nvarchar(max)")]
    public string? GatewayResponse { get; set; }

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("refunded_at")]
    public DateTime? RefundedAt { get; set; }

    [Column("refund_amount", TypeName = "decimal(18,2)")]
    public decimal? RefundAmount { get; set; }

    [MaxLength(200)]
    [Column("failure_reason")]
    public string? FailureReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public static class PaymentStatus
{
    public const string Pending = "PENDING";
    public const string Paid = "PAID";
    public const string Failed = "FAILED";
    public const string Refunded = "REFUNDED";
}

public static class PaymentMode
{
    public const string Card = "CARD";
    public const string Upi = "UPI";
    public const string NetBanking = "NETBANKING";
    public const string Wallet = "WALLET";
}