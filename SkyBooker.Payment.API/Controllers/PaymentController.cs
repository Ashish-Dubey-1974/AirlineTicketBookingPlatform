using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.Payment.API.DTOs;
using SkyBooker.Payment.API.Services;

namespace SkyBooker.Payment.API.Controllers;

[ApiController]
[Route("api/payments")]
[Produces("application/json")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService paymentService,
        IConfiguration config,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _config = config;
        _logger = logger;
    }

    // POST /api/payments/initiate - Initiate payment
    [HttpPost("initiate")]
    [Authorize]
    [ProducesResponseType(typeof(RazorpayOrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var result = await _paymentService.InitiatePaymentAsync(dto, userId.Value);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message, StatusCode = 400 });
        }
    }

    // POST /api/payments/webhook - Razorpay/Stripe webhook
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Webhook()
    {
        string payload;
        using (var reader = new StreamReader(Request.Body))
        {
            payload = await reader.ReadToEndAsync();
        }

        var signature = Request.Headers["X-Razorpay-Signature"].ToString();
        var webhookSecret = _config["Razorpay:WebhookSecret"] ?? "test_webhook_secret";

        var isValid = await _paymentService.ProcessWebhookAsync(payload, signature, webhookSecret);

        if (!isValid)
            return Unauthorized(new { message = "Invalid signature" });

        return Ok(new { status = "received" });
    }

    // GET /api/payments/booking/{bookingId} - Get payment by booking
    [HttpGet("booking/{bookingId}")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentByBooking(string bookingId)
    {
        var payment = await _paymentService.GetPaymentByBookingIdAsync(bookingId);
        if (payment == null)
            return NotFound(new ErrorResponseDto { Message = $"No payment found for booking {bookingId}", StatusCode = 404 });
        return Ok(payment);
    }

    // GET /api/payments/{paymentId} - Get payment by ID
    [HttpGet("{paymentId}")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentById(string paymentId)
    {
        var payment = await _paymentService.GetPaymentByPaymentIdAsync(paymentId);
        if (payment == null)
            return NotFound(new ErrorResponseDto { Message = $"Payment {paymentId} not found", StatusCode = 404 });
        return Ok(payment);
    }

    // GET /api/payments/user/me - Get my payments
    [HttpGet("user/me")]
    [Authorize]
    [ProducesResponseType(typeof(IList<PaymentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPayments()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var payments = await _paymentService.GetPaymentsByUserIdAsync(userId.Value);
        return Ok(payments);
    }

    // POST /api/payments/refund - Refund payment (Admin only)
    [HttpPost("refund")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefundPayment([FromBody] RefundDto dto)
    {
        try
        {
            var result = await _paymentService.RefundPaymentAsync(dto.PaymentId, dto.Amount);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponseDto { Message = ex.Message, StatusCode = 404 });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message, StatusCode = 400 });
        }
    }

    // GET /api/payments/receipt/{paymentId} - Download payment receipt
    [HttpGet("receipt/{paymentId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadReceipt(string paymentId)
    {
        try
        {
            var receiptBytes = await _paymentService.GenerateReceiptAsync(paymentId);
            return File(receiptBytes, "application/pdf", $"receipt_{paymentId}.pdf");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponseDto { Message = ex.Message, StatusCode = 404 });
        }
    }

    // GET /api/payments/revenue - Get revenue (Admin only)
    [HttpGet("revenue")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenue([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var revenue = await _paymentService.GetRevenueAsync(from, to);
        return Ok(new { from, to, revenue, currency = "INR" });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("userId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}