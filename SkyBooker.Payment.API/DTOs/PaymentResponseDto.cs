namespace SkyBooker.Payment.API.DTOs;

public class PaymentResponseDto
{
    public string PaymentId { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentMode { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RazorpayOrderResponseDto
{
    public string OrderId { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class PaymentReceiptDto
{
    public string PaymentId { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public string PnrCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
}