using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.Bookings.API.DTOs;
using SkyBooker.Bookings.API.Services;
using System.Security.Claims;

namespace SkyBooker.Bookings.API.Controllers;

[ApiController]
[Route("api/bookings")]
[Produces("application/json")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingController> _logger;

    public BookingController(IBookingService bookingService, ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    // POST /api/bookings - Create a new booking
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        
        try
        {
            var booking = await _bookingService.CreateBookingAsync(userId.Value, dto);
            return CreatedAtAction(nameof(GetBookingById), new { id = booking.BookingId }, booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message, StatusCode = 400 });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new ErrorResponseDto { Message = $"Service unavailable: {ex.Message}", StatusCode = 502 });
        }
    }

    // GET /api/bookings/{id} - Get booking by ID
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingById(string id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null)
            return NotFound(new ErrorResponseDto { Message = $"Booking {id} not found", StatusCode = 404 });
        
        // Verify ownership or admin
        var userId = GetCurrentUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (booking.UserId != userId && userRole != "ADMIN")
            return Forbid();
        
        return Ok(booking);
    }

    // GET /api/bookings/pnr/{pnr} - Get booking by PNR (public)
    [HttpGet("pnr/{pnr}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingByPnr(string pnr)
    {
        var booking = await _bookingService.GetBookingByPnrAsync(pnr);
        if (booking == null)
            return NotFound(new ErrorResponseDto { Message = $"Booking with PNR {pnr} not found", StatusCode = 404 });
        
        return Ok(booking);
    }

    // GET /api/bookings/user/me - Get current user's bookings
    [HttpGet("user/me")]
    [Authorize]
    [ProducesResponseType(typeof(IList<BookingResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBookings()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        
        var bookings = await _bookingService.GetBookingsByUserAsync(userId.Value);
        return Ok(bookings);
    }

    // GET /api/bookings/user/me/upcoming - Get upcoming bookings
    [HttpGet("user/me/upcoming")]
    [Authorize]
    [ProducesResponseType(typeof(IList<BookingResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpcomingBookings()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        
        var bookings = await _bookingService.GetUpcomingBookingsAsync(userId.Value);
        return Ok(bookings);
    }

    // PUT /api/bookings/{id}/cancel - Cancel a booking
    [HttpPut("{id}/cancel")]
    [Authorize]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBooking(string id, [FromBody] string? reason = null)
    {
        try
        {
            var booking = await _bookingService.CancelBookingAsync(id, reason);
            return Ok(booking);
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

    // PUT /api/bookings/{id}/status - Update status (Admin/Staff only)
    [HttpPut("{id}/status")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] string status)
    {
        try
        {
            var booking = await _bookingService.UpdateStatusAsync(id, status);
            return Ok(booking);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message, StatusCode = 400 });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message, StatusCode = 400 });
        }
    }

    // PUT /api/bookings/{id}/confirm - Confirm booking (called by Payment Service)
    [HttpPut("{id}/confirm")]
    [Authorize]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmBooking(string id, [FromBody] string paymentId)
    {
        var booking = await _bookingService.ConfirmBookingAsync(id, paymentId);
        return Ok(booking);
    }

    // POST /api/bookings/calculate-fare - Calculate fare without booking
    [HttpPost("calculate-fare")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FareSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateFare(
        [FromQuery] int flightId,
        [FromQuery] int passengers = 1,
        [FromQuery] int extraBaggageKg = 0,
        [FromQuery] bool hasMeal = false)
    {
        var fare = await _bookingService.CalculateFareAsync(flightId, passengers, extraBaggageKg, hasMeal);
        return Ok(fare);
    }

    // POST /api/bookings/{id}/addons - Add add-ons to existing booking
    [HttpPost("{id}/addons")]
    [Authorize]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddAddOns(string id, [FromBody] AddOnDto dto)
    {
        if (dto.BookingId != id)
            return BadRequest(new ErrorResponseDto { Message = "Booking ID mismatch", StatusCode = 400 });
        
        try
        {
            var booking = await _bookingService.AddAddOnsAsync(id, dto);
            return Ok(booking);
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

    // GET /api/bookings/flight/{flightId} - Get bookings by flight (Staff only)
    [HttpGet("flight/{flightId}")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(typeof(IList<BookingResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBookingsByFlight(int flightId)
    {
        // This would need to be implemented in repository
        return Ok(new List<BookingResponseDto>());
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("userId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}