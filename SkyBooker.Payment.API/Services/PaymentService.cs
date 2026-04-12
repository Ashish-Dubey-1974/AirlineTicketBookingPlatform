using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SkyBooker.Payment.API.Data;
using SkyBooker.Payment.API.DTOs;
using SkyBooker.Payment.API.Entities;

namespace SkyBooker.Payment.API.Services;

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentService> _logger;
    private readonly IConfiguration _config;

    public PaymentService(
        PaymentDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<PaymentService> logger,
        IConfiguration config)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task<RazorpayOrderResponseDto> InitiatePaymentAsync(InitiatePaymentDto dto, int userId)
    {
        // Check if payment already exists for this booking
        var existingPayment = await _context.Payments
            .FirstOrDefaultAsync(p => p.BookingId == dto.BookingId);

        if (existingPayment != null && existingPayment.Status == PaymentStatus.Paid)
        {
            throw new InvalidOperationException($"Payment already completed for booking {dto.BookingId}");
        }

        // Create payment record
        var payment = new Payments
        {
            PaymentId = Guid.NewGuid().ToString(),
            BookingId = dto.BookingId,
            UserId = userId,
            Amount = dto.Amount,
            Currency = "INR",
            Status = PaymentStatus.Pending,
            PaymentMode = dto.PaymentMode,
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Simulate Razorpay order creation (In production, call actual Razorpay API)
        var razorpayOrderId = $"order_{Guid.NewGuid().ToString("N").Substring(0, 15)}";

        _logger.LogInformation("Payment initiated: PaymentId={PaymentId}, OrderId={OrderId}, Amount={Amount}", 
            payment.PaymentId, razorpayOrderId, dto.Amount);

        return new RazorpayOrderResponseDto
        {
            OrderId = razorpayOrderId,
            PaymentId = payment.PaymentId,
            Amount = dto.Amount,
            Currency = "INR",
            Status = "created"
        };
    }

    public async Task<bool> ProcessWebhookAsync(string payload, string signature, string webhookSecret)
    {
        // Step 1: Verify HMAC-SHA256 signature
        var expectedSignature = ComputeHmacSha256(webhookSecret, payload);
        
        if (!expectedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Webhook signature verification failed!");
            return false;
        }

        _logger.LogInformation("Webhook signature verified successfully");

        // Step 2: Parse payload
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var eventType = root.GetProperty("event").GetString();

        if (eventType == "payment.captured")
        {
            var paymentEntity = root.GetProperty("payload").GetProperty("payment").GetProperty("entity");
            var razorpayPaymentId = paymentEntity.GetProperty("id").GetString();
            var razorpayOrderId = paymentEntity.GetProperty("order_id").GetString();
            var amount = paymentEntity.GetProperty("amount").GetDecimal() / 100; // Convert paise to rupees
            var method = paymentEntity.GetProperty("method").GetString();

            // Find payment by order reference (in production, store order_id in Payment table)
            // For now, we need to find by some reference
            var payment = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Pending)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (payment != null)
            {
                payment.Status = PaymentStatus.Paid;
                payment.TransactionId = razorpayPaymentId;
                payment.GatewayResponse = payload;
                payment.PaidAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment confirmed: PaymentId={PaymentId}, TransactionId={TransactionId}", 
                    payment.PaymentId, razorpayPaymentId);

                // Call Booking API to confirm booking
                await ConfirmBookingAsync(payment.BookingId, payment.PaymentId);
            }
        }
        else if (eventType == "payment.failed")
        {
            var paymentEntity = root.GetProperty("payload").GetProperty("payment").GetProperty("entity");
            var razorpayPaymentId = paymentEntity.GetProperty("id").GetString();

            var payment = await _context.Payments
                .Where(p => p.TransactionId == razorpayPaymentId)
                .FirstOrDefaultAsync();

            if (payment != null)
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = "Payment failed at gateway";
                payment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogWarning("Payment failed: PaymentId={PaymentId}", payment.PaymentId);
            }
        }

        return true;
    }

    public async Task<PaymentResponseDto?> GetPaymentByBookingIdAsync(string bookingId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.BookingId == bookingId);
        
        return payment == null ? null : MapToResponse(payment);
    }

    public async Task<PaymentResponseDto?> GetPaymentByPaymentIdAsync(string paymentId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        
        return payment == null ? null : MapToResponse(payment);
    }

    public async Task<IList<PaymentResponseDto>> GetPaymentsByUserIdAsync(int userId)
    {
        var payments = await _context.Payments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        
        return payments.Select(MapToResponse).ToList();
    }

    public async Task<PaymentResponseDto> RefundPaymentAsync(string paymentId, decimal? amount = null)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        if (payment == null)
            throw new KeyNotFoundException($"Payment {paymentId} not found");

        if (payment.Status != PaymentStatus.Paid)
            throw new InvalidOperationException($"Cannot refund payment with status {payment.Status}");

        var refundAmount = amount ?? payment.Amount;

        // In production: Call Razorpay Refund API
        // var refund = await razorpayClient.Payment.Refund(payment.TransactionId, refundAmount * 100);

        payment.Status = PaymentStatus.Refunded;
        payment.RefundAmount = refundAmount;
        payment.RefundedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment refunded: PaymentId={PaymentId}, Amount={Amount}", paymentId, refundAmount);

        // Call Booking API to cancel booking
        await CancelBookingAsync(payment.BookingId);

        return MapToResponse(payment);
    }

    public async Task<byte[]> GenerateReceiptAsync(string paymentId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        if (payment == null)
            throw new KeyNotFoundException($"Payment {paymentId} not found");

        // In production: Use QuestPDF to generate PDF
        // For now, return a simple text receipt as byte array
        var receiptText = $@"
SKYBOOKER PAYMENT RECEIPT
========================
Receipt No: {payment.PaymentId}
Date: {payment.PaidAt:dd-MM-yyyy HH:mm}
Booking ID: {payment.BookingId}
Amount: ₹{payment.Amount}
Payment Mode: {payment.PaymentMode}
Transaction ID: {payment.TransactionId}
Status: {payment.Status}

Thank you for booking with SkyBooker!
        ";

        return Encoding.UTF8.GetBytes(receiptText);
    }

    public async Task<decimal> GetRevenueAsync(DateTime from, DateTime to)
    {
        return await _context.Payments
            .Where(p => p.Status == PaymentStatus.Paid && p.PaidAt >= from && p.PaidAt <= to)
            .SumAsync(p => p.Amount);
    }

    // Helper: HMAC-SHA256 for webhook verification
    private static string ComputeHmacSha256(string secret, string payload)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    // Call Booking API to confirm booking after successful payment
    private async Task ConfirmBookingAsync(string bookingId, string paymentId)
    {
        try
        {
            var bookingClient = _httpClientFactory.CreateClient("BookingService");
            var response = await bookingClient.PutAsJsonAsync(
                $"/api/bookings/{bookingId}/confirm", paymentId);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Booking confirmed: {BookingId}", bookingId);
            }
            else
            {
                _logger.LogWarning("Failed to confirm booking: {BookingId}", bookingId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming booking {BookingId}", bookingId);
        }
    }

    // Call Booking API to cancel booking after refund
    private async Task CancelBookingAsync(string bookingId)
    {
        try
        {
            var bookingClient = _httpClientFactory.CreateClient("BookingService");
            var response = await bookingClient.PutAsJsonAsync(
                $"/api/bookings/{bookingId}/cancel", "Payment refunded");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Booking cancelled after refund: {BookingId}", bookingId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", bookingId);
        }
    }

    private static PaymentResponseDto MapToResponse(Payments p) => new()
    {
        PaymentId = p.PaymentId,
        BookingId = p.BookingId,
        UserId = p.UserId,
        Amount = p.Amount,
        Currency = p.Currency,
        Status = p.Status,
        PaymentMode = p.PaymentMode,
        TransactionId = p.TransactionId,
        PaidAt = p.PaidAt,
        RefundedAt = p.RefundedAt,
        RefundAmount = p.RefundAmount,
        FailureReason = p.FailureReason,
        CreatedAt = p.CreatedAt
    };
}