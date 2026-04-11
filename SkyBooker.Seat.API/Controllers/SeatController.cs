// SkyBooker.Seat.API/Controllers/SeatController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.Seat.API.DTOs;
using SkyBooker.Seat.API.Services;

namespace SkyBooker.Seat.API.Controllers;

[ApiController]
[Route("api/seats")]
[Produces("application/json")]
public class SeatController : ControllerBase
{
    private readonly ISeatService _seatService;
    private readonly ILogger<SeatController> _logger;
    
    public SeatController(ISeatService seatService, ILogger<SeatController> logger)
    {
        _seatService = seatService;
        _logger = logger;
    }
    
    [HttpPost("flight/{flightId}/batch")]
    [Authorize(Policy = "StaffOnly")]
    public async Task<IActionResult> AddSeatsForFlight(int flightId, [FromBody] IList<CreateSeatDto> seats)
    {
        await _seatService.AddSeatsForFlightAsync(flightId, seats);
        return CreatedAtAction(nameof(GetSeatMap), new { flightId }, new { message = $"Added {seats.Count} seats" });
    }
    
    [HttpGet("map/{flightId}")]
    [Authorize]
    public async Task<IActionResult> GetSeatMap(int flightId)
    {
        var seats = await _seatService.GetSeatMapAsync(flightId);
        return Ok(seats);
    }
    
    [HttpGet("available/{flightId}")]
    [Authorize]
    public async Task<IActionResult> GetAvailableSeats(int flightId)
    {
        var seats = await _seatService.GetAvailableSeatsAsync(flightId);
        return Ok(seats);
    }
    
    [HttpGet("{seatId}")]
    [Authorize]
    public async Task<IActionResult> GetSeatById(int seatId)
    {
        var seat = await _seatService.GetSeatByIdAsync(seatId);
        if (seat == null) return NotFound();
        return Ok(seat);
    }
    
    [HttpPut("{seatId}/hold")]
    [Authorize]
    public async Task<IActionResult> HoldSeat(int seatId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        
        try
        {
            var seat = await _seatService.HoldSeatAsync(seatId, userId.Value);
            return Ok(seat);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
    
    [HttpPut("{seatId}/release")]
    [Authorize]
    public async Task<IActionResult> ReleaseSeat(int seatId)
    {
        await _seatService.ReleaseSeatAsync(seatId);
        return Ok(new { message = "Seat released" });
    }
    
    [HttpGet("count/{flightId}/{seatClass}")]
    [AllowAnonymous]
    public async Task<IActionResult> CountAvailableByClass(int flightId, string seatClass)
    {
        var count = await _seatService.CountAvailableByClassAsync(flightId, seatClass);
        return Ok(new { flightId, seatClass, availableCount = count });
    }
    
    [HttpDelete("flight/{flightId}")]
    [Authorize(Policy = "StaffOnly")]
    public async Task<IActionResult> DeleteSeatsForFlight(int flightId)
    {
        await _seatService.DeleteSeatsForFlightAsync(flightId);
        return NoContent();
    }
    
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("userId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}