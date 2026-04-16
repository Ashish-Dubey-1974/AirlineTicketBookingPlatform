using SkyBooker.Payment.API.DTOs;

namespace SkyBooker.Payment.API.Services;

public interface IPaymentService
{
    // Payment initiation
    Task<RazorpayOrderResponseDto> InitiatePaymentAsync(InitiatePaymentDto dto, int userId);
    Task<PaymentResponseDto> CompleteMockPaymentAsync(string paymentId, int userId);
    
    // Webhook processing
    Task<bool> ProcessWebhookAsync(string payload, string signature, string webhookSecret);
    
    // Payment queries
    Task<PaymentResponseDto?> GetPaymentByBookingIdAsync(string bookingId);
    Task<PaymentResponseDto?> GetPaymentByPaymentIdAsync(string paymentId);
    Task<IList<PaymentResponseDto>> GetPaymentsByUserIdAsync(int userId);
    
    // Refund
    Task<PaymentResponseDto> RefundPaymentAsync(string paymentId, decimal? amount = null);
    
    // Receipt generation (QuestPDF)
    Task<byte[]> GenerateReceiptAsync(string paymentId);
    
    // Revenue
    Task<decimal> GetRevenueAsync(DateTime from, DateTime to);
}
