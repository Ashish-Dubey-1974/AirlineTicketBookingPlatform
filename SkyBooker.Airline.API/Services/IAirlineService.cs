using SkyBooker.Airline.DTOs;
using SkyBooker.Airline.Entities;

namespace SkyBooker.Airline.Services;

/// <summary>
/// Service interface for all Airline and Airport business operations.
/// Implemented by AirlineService.cs.
/// Registered as Scoped in Program.cs.
/// </summary>
public interface IAirlineService
{
    // ── Airline operations ────────────────────────────────────────────────────

    Task<AirlineResponseDto> CreateAirline(CreateAirlineDto dto);
    Task<AirlineResponseDto?> GetAirlineById(int airlineId);
    Task<AirlineResponseDto?> GetAirlineByIata(string iataCode);
    Task<IList<AirlineResponseDto>> GetAllAirlines();
    Task<IList<AirlineResponseDto>> GetActiveAirlines();
    Task<AirlineResponseDto?> UpdateAirline(int airlineId, UpdateAirlineDto dto);
    Task DeactivateAirline(int airlineId);
    Task ReactivateAirline(int airlineId);

    // ── Airport operations ────────────────────────────────────────────────────

    Task<AirportResponseDto> CreateAirport(CreateAirportDto dto);
    Task<AirportResponseDto?> GetAirportById(int airportId);
    Task<AirportResponseDto?> GetAirportByIata(string iataCode);
    Task<IList<AirportResponseDto>> GetAllAirports();
    Task<IList<AirportResponseDto>> GetAirportsByCity(string city);
    Task<IList<AirportResponseDto>> SearchAirports(string query);
    Task<AirportResponseDto?> UpdateAirport(int airportId, UpdateAirportDto dto);

    // ── Airline-Airport linking ───────────────────────────────────────────────

    Task LinkAirportToAirline(int airlineId, int airportId);
    Task UnlinkAirportFromAirline(int airlineId, int airportId);
    Task<IList<AirportResponseDto>> GetAirportsByAirline(int airlineId);

    // ── Mapping helpers ───────────────────────────────────────────────────────

    AirlineResponseDto MapToAirlineDto(Entities.Airline airline);
    AirportResponseDto MapToAirportDto(Airport airport);
}
