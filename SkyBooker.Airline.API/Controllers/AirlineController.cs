using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.Airline.DTOs;
using SkyBooker.Airline.Services;

namespace SkyBooker.Airline.Controllers;

/// <summary>
/// REST API for Airline management (CRUD + deactivation + airport linking).
/// Admin-only write operations; public read for active airlines.
/// </summary>
[ApiController]
[Route("api/airlines")]
[Produces("application/json")]
public class AirlineController : ControllerBase
{
    private readonly IAirlineService _airlineService;
    private readonly ILogger<AirlineController> _logger;

    public AirlineController(IAirlineService airlineService, ILogger<AirlineController> logger)
    {
        _airlineService = airlineService;
        _logger         = logger;
    }

    // ── POST /api/airlines ────────────────────────────────────────────────────
    /// <summary>Create a new airline — Admin only</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(AirlineResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAirline([FromBody] CreateAirlineDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponseDto
            {
                Message    = "Validation failed",
                Details    = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors).Select(e => e.ErrorMessage)),
                StatusCode = 400
            });

        try
        {
            var result = await _airlineService.CreateAirline(dto);
            return CreatedAtAction(nameof(GetAirlineById), new { id = result.AirlineId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponseDto { Message = ex.Message, StatusCode = 409 });
        }
    }

    // ── GET /api/airlines ─────────────────────────────────────────────────────
    /// <summary>Get all airlines (active only for public; all for Admin)</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IList<AirlineResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAirlines([FromQuery] bool activeOnly = true)
    {
        var airlines = activeOnly
            ? await _airlineService.GetActiveAirlines()
            : await _airlineService.GetAllAirlines();

        return Ok(airlines);
    }

    // ── GET /api/airlines/{id} ────────────────────────────────────────────────
    /// <summary>Get airline by ID</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AirlineResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAirlineById(int id)
    {
        var airline = await _airlineService.GetAirlineById(id);
        if (airline == null)
            return NotFound(new ErrorResponseDto { Message = $"Airline {id} not found.", StatusCode = 404 });

        return Ok(airline);
    }

    // ── GET /api/airlines/iata/{code} ─────────────────────────────────────────
    /// <summary>Get airline by IATA code (e.g. AI, 6E)</summary>
    [HttpGet("iata/{code}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AirlineResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAirlineByIata(string code)
    {
        var airline = await _airlineService.GetAirlineByIata(code);
        if (airline == null)
            return NotFound(new ErrorResponseDto { Message = $"No airline found with IATA code '{code}'.", StatusCode = 404 });

        return Ok(airline);
    }

    // ── PUT /api/airlines/{id} ────────────────────────────────────────────────
    /// <summary>Update airline details — Admin only</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(AirlineResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAirline(int id, [FromBody] UpdateAirlineDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updated = await _airlineService.UpdateAirline(id, dto);
        if (updated == null)
            return NotFound(new ErrorResponseDto { Message = $"Airline {id} not found.", StatusCode = 404 });

        return Ok(updated);
    }

    // ── PUT /api/airlines/{id}/deactivate ─────────────────────────────────────
    /// <summary>Deactivate an airline (hides it from new bookings) — Admin only</summary>
    [HttpPut("{id:int}/deactivate")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateAirline(int id)
    {
        try
        {
            await _airlineService.DeactivateAirline(id);
            return Ok(new { message = $"Airline {id} deactivated successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponseDto { Message = ex.Message, StatusCode = 404 });
        }
    }

    // ── PUT /api/airlines/{id}/reactivate ─────────────────────────────────────
    /// <summary>Reactivate a deactivated airline — Admin only</summary>
    [HttpPut("{id:int}/reactivate")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateAirline(int id)
    {
        try
        {
            await _airlineService.ReactivateAirline(id);
            return Ok(new { message = $"Airline {id} reactivated successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponseDto { Message = ex.Message, StatusCode = 404 });
        }
    }

    // ── POST /api/airlines/{airlineId}/airports/{airportId} ───────────────────
    /// <summary>Link an airport to an airline — Admin only</summary>
    [HttpPost("{airlineId:int}/airports/{airportId:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LinkAirport(int airlineId, int airportId)
    {
        try
        {
            await _airlineService.LinkAirportToAirline(airlineId, airportId);
            return Ok(new { message = $"Airport {airportId} linked to Airline {airlineId}." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponseDto { Message = ex.Message, StatusCode = 404 });
        }
    }

    // ── DELETE /api/airlines/{airlineId}/airports/{airportId} ─────────────────
    /// <summary>Unlink an airport from an airline — Admin only</summary>
    [HttpDelete("{airlineId:int}/airports/{airportId:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UnlinkAirport(int airlineId, int airportId)
    {
        await _airlineService.UnlinkAirportFromAirline(airlineId, airportId);
        return Ok(new { message = $"Airport {airportId} unlinked from Airline {airlineId}." });
    }

    // ── GET /api/airlines/{airlineId}/airports ────────────────────────────────
    /// <summary>Get all airports served by an airline</summary>
    [HttpGet("{airlineId:int}/airports")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IList<AirportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAirportsByAirline(int airlineId)
    {
        var airports = await _airlineService.GetAirportsByAirline(airlineId);
        return Ok(airports);
    }
}
