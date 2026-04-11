using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.Flights.API.DTOs;
using SkyBooker.Flights.API.Services;

namespace SkyBooker.Flights.API.Controllers;

[ApiController]
[Route("api/flights")]
[Produces("application/json")]
public class FlightController : ControllerBase
{
    private readonly IFlightService _flightService;
    private readonly ILogger<FlightController> _logger;

    public FlightController(IFlightService flightService, ILogger<FlightController> logger)
    {
        _flightService = flightService;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────
    // POST /api/flights
    // Airline Staff only — add a new flight
    // ──────────────────────────────────────────────────────────────────
    /// <summary>Add a new flight. Airline Staff only.</summary>
    [HttpPost]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(typeof(FlightResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddFlight([FromBody] FlightCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _flightService.AddFlightAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = response.FlightId }, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("AddFlight conflict: {Message}", ex.Message);
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // GET /api/flights/{id}
    // Public — get flight by ID
    // ──────────────────────────────────────────────────────────────────
    /// <summary>Get flight details by ID.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FlightResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var flight = await _flightService.GetFlightByIdAsync(id);
        if (flight is null)
            return NotFound(new { message = $"Flight with ID {id} not found." });
        return Ok(flight);
    }

    // ──────────────────────────────────────────────────────────────────
    // GET /api/flights/search
    // Public — one-way search with optional filters
    // ?origin=DEL&dest=BOM&date=2026-05-01&passengers=2
    //   &class=Economy&airline=AI&maxPrice=5000&departureTimeRange=Morning
    // ──────────────────────────────────────────────────────────────────
    /// <summary>Search one-way flights with filters.</summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IList<FlightResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string origin,
        [FromQuery] string dest,
        [FromQuery] DateTime date,
        [FromQuery] int passengers = 1,
        [FromQuery] string? @class = null,
        [FromQuery] string? airline = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? departureTimeRange = null,
        [FromQuery] int? stops = null)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(dest))
            return BadRequest(new { message = "origin and dest are required." });

        if (passengers < 1)
            return BadRequest(new { message = "passengers must be at least 1." });

        var request = new FlightSearchRequest
        {
            Origin             = origin,
            Destination        = dest,
            DepartureDate      = date,
            Passengers         = passengers,
            Class              = @class,
            Airline            = airline,
            MaxPrice           = maxPrice,
            DepartureTimeRange = departureTimeRange,
            Stops              = stops
        };

        var flights = await _flightService.SearchFlightsAsync(request);
        return Ok(flights);
    }

    // ──────────────────────────────────────────────────────────────────
    // GET /api/flights/roundtrip
    // Public — round-trip search returning {"outbound":[...], "return":[...]}
    // ?origin=DEL&dest=BOM&departure=2026-05-01&return=2026-05-10&passengers=2
    // ──────────────────────────────────────────────────────────────────
    /// <summary>Search round-trip flights. Returns outbound and return legs.</summary>
    [HttpGet("roundtrip")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Dictionary<string, IList<FlightResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchRoundTrip(
        [FromQuery] string origin,
        [FromQuery] string dest,
        [FromQuery] DateTime departure,
        [FromQuery] DateTime @return,
        [FromQuery] int passengers = 1,
        [FromQuery] string? @class = null)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(dest))
            return BadRequest(new { message = "origin and dest are required." });

        if (@return <= departure)
            return BadRequest(new { message = "Return date must be after departure date." });

        var request = new RoundTripSearchRequest
        {
            Origin        = origin,
            Destination   = dest,
            DepartureDate = departure,
            ReturnDate    = @return,
            Passengers    = passengers,
            Class         = @class
        };

        var result = await _flightService.SearchRoundTripAsync(request);
        return Ok(result);
    }

    // ──────────────────────────────────────────────────────────────────
    // PUT /api/flights/{id}
    // Airline Staff only — update flight schedule fields
    // ──────────────────────────────────────────────────────────────────
    /// <summary>Update flight schedule fields. Airline Staff only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(typeof(FlightResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFlight(int id, [FromBody] FlightUpdateDto dto)
    {
        try
        {
            var updated = await _flightService.UpdateFlightAsync(id, dto);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // PUT /api/flights/{id}/status
    // Airline Staff only — update status (SCHEDULED/DELAYED/CANCELLED/DEPARTED/ARRIVED)
    // Body: { "status": "DELAYED" }
    // ──────────────────────────────────────────────────────────────────
    /// <summary>Update flight status. Airline Staff only.</summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] FlightStatusUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _flightService.UpdateStatusAsync(id, dto.Status);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // DELETE /api/flights/{id}
    // Airline Staff only — delete if no active bookings
    // ──────────────────────────────────────────────────────────────────
    /// <summary>Delete a flight (only if no active bookings). Airline Staff only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFlight(int id)
    {
        try
        {
            await _flightService.DeleteFlightAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // GET /api/flights/airline/{id}
    // Airline Staff — all flights for their airline
    // ──────────────────────────────────────────────────────────────────
    /// <summary>Get all flights for a specific airline. Airline Staff only.</summary>
    [HttpGet("airline/{id:int}")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(typeof(IList<FlightResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByAirline(int id)
    {
        var flights = await _flightService.GetFlightsByAirlineAsync(id);
        return Ok(flights);
    }

    // ──────────────────────────────────────────────────────────────────
    // PUT /api/flights/{id}/decrement-seats
    // Internal — called by Booking service during booking creation
    // ──────────────────────────────────────────────────────────────────
    /// <summary>Atomically decrement available seat count. Internal use (Booking service).</summary>
    [HttpPut("{id:int}/decrement-seats")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DecrementSeats(int id, [FromQuery] int count = 1)
    {
        var success = await _flightService.DecrementSeatsAsync(id, count);
        if (!success)
            return Conflict(new { message = "Not enough available seats or flight not found." });
        return NoContent();
    }

    // ──────────────────────────────────────────────────────────────────
    // PUT /api/flights/{id}/increment-seats
    // Internal — called by Booking service during cancellation
    // ──────────────────────────────────────────────────────────────────
    /// <summary>Atomically increment available seat count. Internal use (Booking service).</summary>
    [HttpPut("{id:int}/increment-seats")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> IncrementSeats(int id, [FromQuery] int count = 1)
    {
        await _flightService.IncrementSeatsAsync(id, count);
        return NoContent();
    }
}
