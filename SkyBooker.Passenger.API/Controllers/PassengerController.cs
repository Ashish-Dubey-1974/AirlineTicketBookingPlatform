using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.Passenger.API.DTOs;
using SkyBooker.Passenger.API.Services;

namespace SkyBooker.Passenger.API.Controllers;

[ApiController]
[Route("api/passengers")]
[Produces("application/json")]
public class PassengerController : ControllerBase
{
    private readonly IPassengerService _passengerService;
    private readonly ILogger<PassengerController> _logger;

    public PassengerController(IPassengerService passengerService, ILogger<PassengerController> logger)
    {
        _passengerService = passengerService;
        _logger = logger;
    }

    // POST /api/passengers - Add passenger
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PassengerResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddPassenger([FromBody] PassengerRequestDto dto)
    {
        try
        {
            var passenger = await _passengerService.AddPassengerAsync(dto);
            return CreatedAtAction(nameof(GetPassengerById), new { id = passenger.PassengerId }, passenger);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ErrorResponseDto 
            { 
                Message = "Validation failed", 
                Details = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)),
                StatusCode = 400 
            });
        }
    }

    // GET /api/passengers/{id} - Get passenger by ID
    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(PassengerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPassengerById(int id)
    {
        var passenger = await _passengerService.GetPassengerByIdAsync(id);
        if (passenger == null)
            return NotFound(new ErrorResponseDto { Message = $"Passenger {id} not found", StatusCode = 404 });
        return Ok(passenger);
    }

    // GET /api/passengers/booking/{bookingId} - Get all passengers for a booking
    [HttpGet("booking/{bookingId}")]
    [Authorize]
    [ProducesResponseType(typeof(IList<PassengerResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPassengersByBooking(string bookingId)
    {
        var passengers = await _passengerService.GetPassengersByBookingAsync(bookingId);
        return Ok(passengers);
    }

    // GET /api/passengers/passport/{passportNumber} - Get passenger by passport
    [HttpGet("passport/{passportNumber}")]
    [Authorize]
    [ProducesResponseType(typeof(PassengerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPassportNumber(string passportNumber)
    {
        var passenger = await _passengerService.GetByPassportNumberAsync(passportNumber);
        if (passenger == null)
            return NotFound(new ErrorResponseDto { Message = $"Passenger with passport {passportNumber} not found", StatusCode = 404 });
        return Ok(passenger);
    }

    // PUT /api/passengers/{id} - Update passenger
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(PassengerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePassenger(int id, [FromBody] PassengerRequestDto dto)
    {
        try
        {
            var passenger = await _passengerService.UpdatePassengerAsync(id, dto);
            return Ok(passenger);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponseDto { Message = ex.Message, StatusCode = 404 });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ErrorResponseDto 
            { 
                Message = "Validation failed", 
                Details = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)),
                StatusCode = 400 
            });
        }
    }

    // PUT /api/passengers/{id}/assign-seat - Assign seat to passenger
    [HttpPut("{id:int}/assign-seat")]
    [Authorize]
    [ProducesResponseType(typeof(PassengerResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignSeat(int id, [FromBody] AssignSeatDto dto)
    {
        if (id != dto.PassengerId)
            return BadRequest(new ErrorResponseDto { Message = "Passenger ID mismatch", StatusCode = 400 });

        var passenger = await _passengerService.AssignSeatAsync(id, dto.SeatId, dto.SeatNumber);
        return Ok(passenger);
    }

    // POST /api/passengers/{id}/checkin - Web check-in
    [HttpPost("{id:int}/checkin")]
    [Authorize]
    [ProducesResponseType(typeof(PassengerResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> WebCheckIn(int id, [FromBody] WebCheckInDto dto)
    {
        if (id != dto.PassengerId)
            return BadRequest(new ErrorResponseDto { Message = "Passenger ID mismatch", StatusCode = 400 });

        try
        {
            var passenger = await _passengerService.WebCheckInAsync(id, dto.NewSeatId, dto.NewSeatNumber);
            return Ok(passenger);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message, StatusCode = 400 });
        }
    }

    // GET /api/passengers/count/{bookingId} - Get passenger count for booking
    [HttpGet("count/{bookingId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPassengerCount(string bookingId)
    {
        var count = await _passengerService.GetPassengerCountByBookingAsync(bookingId);
        return Ok(count);
    }

    // DELETE /api/passengers/booking/{bookingId} - Delete all passengers for booking
    [HttpDelete("booking/{bookingId}")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteByBooking(string bookingId)
    {
        await _passengerService.DeleteByBookingAsync(bookingId);
        return NoContent();
    }
}
