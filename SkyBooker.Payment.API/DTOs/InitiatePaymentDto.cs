using System.ComponentModel.DataAnnotations;

namespace SkyBooker.Payment.API.DTOs;

public class InitiatePaymentDto
{
    [Required]
    [MaxLength(36)]
    public string BookingId { get; set; } = string.Empty;

    [Required]
    [Range(1, 1000000)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(20)]
    [RegularExpression("CARD|UPI|NETBANKING|WALLET")]
    public string PaymentMode { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CardToken { get; set; }

    [MaxLength(50)]
    public string? Vpa { get; set; }  // For UPI

    [MaxLength(50)]
    public string? BankCode { get; set; }  // For NetBanking
}

public class RefundDto
{
    [Required]
    [MaxLength(36)]
    public string PaymentId { get; set; } = string.Empty;

    [Range(1, 1000000)]
    public decimal? Amount { get; set; }  // Null = full refund
}