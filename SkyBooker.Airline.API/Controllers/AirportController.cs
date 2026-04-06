using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.Airline.DTOs;
using SkyBooker.Airline.Services;

namespace SkyBooker.Airline.Controllers;

/// <summary>
/// REST API for Airport management (CRUD + search/autocomplete).
/// Search endpoints are public (used by Home page autocomplete).
/// Write operations are Admin-only.
/// </summary>
[ApiController]
[Route("api/airports")]
[Produces("application/json")]
public class AirportController : ControllerBase
{
    private readonly IAirlineService _airlineService;
    private readonly ILogger<AirportController> _logger;

    public AirportController(IAirlineService airlineService, ILogger<AirportController> logger)
    {
        _airlineService = airlineService;
        _logger         = logger;
    }

    // ── POST /api/airports ────────────────────────────────────────────────────
    /// <summary>Create a new airport — Admin only</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(AirportResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAirport([FromBody] CreateAirportDto dto)
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
            var result = await _airlineService.CreateAirport(dto);
            return CreatedAtAction(nameof(GetAirportById), new { id = result.AirportId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponseDto { Message = ex.Message, StatusCode = 409 });
        }
    }

    // ── GET /api/airports ─────────────────────────────────────────────────────
    /// <summary>Get all airports</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IList<AirportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAirports()
    {
        var airports = await _airlineService.GetAllAirports();
        return Ok(airports);
    }

    // ── GET /api/airports/{id} ────────────────────────────────────────────────
    /// <summary>Get airport by ID</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AirportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAirportById(int id)
    {
        var airport = await _airlineService.GetAirportById(id);
        if (airport == null)
            return NotFound(new ErrorResponseDto { Message = $"Airport {id} not found.", StatusCode = 404 });

        return Ok(airport);
    }

    // ── GET /api/airports/iata/{code} ─────────────────────────────────────────
    /// <summary>Get airport by IATA code (e.g. DEL, BOM, BLR)</summary>
    [HttpGet("iata/{code}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AirportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAirportByIata(string code)
    {
        var airport = await _airlineService.GetAirportByIata(code.ToUpper());
        if (airport == null)
            return NotFound(new ErrorResponseDto { Message = $"No airport found with IATA code '{code}'.", StatusCode = 404 });

        return Ok(airport);
    }

    // ── GET /api/airports/search ──────────────────────────────────────────────
    /// <summary>
    /// Search airports by name, IATA code, or city — used for Home page autocomplete.
    /// Minimum 2 characters required. Returns up to 10 results.
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IList<AirportResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchAirports([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new ErrorResponseDto
            {
                Message    = "Search query must be at least 2 characters.",
                StatusCode = 400
            });

        var results = await _airlineService.SearchAirports(q);
        return Ok(results);
    }

    // ── GET /api/airports/city/{city} ─────────────────────────────────────────
    /// <summary>Get all airports in a city</summary>
    [HttpGet("city/{city}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IList<AirportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCity(string city)
    {
        var airports = await _airlineService.GetAirportsByCity(city);
        return Ok(airports);
    }

    // ── PUT /api/airports/{id} ────────────────────────────────────────────────
    /// <summary>Update airport details — Admin only</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(AirportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAirport(int id, [FromBody] UpdateAirportDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updated = await _airlineService.UpdateAirport(id, dto);
        if (updated == null)
            return NotFound(new ErrorResponseDto { Message = $"Airport {id} not found.", StatusCode = 404 });

        return Ok(updated);
    }
}
