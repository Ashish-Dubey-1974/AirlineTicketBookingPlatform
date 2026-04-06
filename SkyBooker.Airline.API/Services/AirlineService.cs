using SkyBooker.Airline.DTOs;
using SkyBooker.Airline.Entities;
using SkyBooker.Airline.Repositories;

namespace SkyBooker.Airline.Services;

/// <summary>
/// Business logic for Airline and Airport management.
/// All write operations validate uniqueness constraints before persisting.
/// Implements IAirlineService — registered as Scoped in Program.cs.
/// </summary>
public class AirlineService : IAirlineService
{
    private readonly IAirlineRepository _repo;
    private readonly ILogger<AirlineService> _logger;

    public AirlineService(IAirlineRepository repo, ILogger<AirlineService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    // ── CREATE AIRLINE ────────────────────────────────────────────────────────
    public async Task<AirlineResponseDto> CreateAirline(CreateAirlineDto dto)
    {
        // Ensure IataCode is always stored in uppercase
        dto.IataCode = dto.IataCode.ToUpper().Trim();

        if (await _repo.ExistsByAirlineIataCode(dto.IataCode))
            throw new InvalidOperationException(
                $"An airline with IATA code '{dto.IataCode}' already exists.");

        var airline = new Entities.Airline
        {
            Name         = dto.Name.Trim(),
            IataCode     = dto.IataCode,
            IcaoCode     = dto.IcaoCode?.ToUpper().Trim(),
            LogoUrl      = dto.LogoUrl?.Trim(),
            Country      = dto.Country?.Trim(),
            ContactEmail = dto.ContactEmail?.Trim().ToLower(),
            ContactPhone = dto.ContactPhone?.Trim(),
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };

        var saved = await _repo.SaveAirline(airline);
        _logger.LogInformation("Airline created: {Name} ({IataCode}) — AirlineId={AirlineId}",
            saved.Name, saved.IataCode, saved.AirlineId);

        return MapToAirlineDto(saved);
    }

    // ── GET AIRLINE ───────────────────────────────────────────────────────────
    public async Task<AirlineResponseDto?> GetAirlineById(int airlineId)
    {
        var airline = await _repo.FindByAirlineId(airlineId);
        return airline == null ? null : MapToAirlineDto(airline);
    }

    public async Task<AirlineResponseDto?> GetAirlineByIata(string iataCode)
    {
        var airline = await _repo.FindByIataCode(iataCode.ToUpper());
        return airline == null ? null : MapToAirlineDto(airline);
    }

    public async Task<IList<AirlineResponseDto>> GetAllAirlines()
    {
        var airlines = await _repo.FindAllAirlines();
        return airlines.Select(MapToAirlineDto).ToList();
    }

    public async Task<IList<AirlineResponseDto>> GetActiveAirlines()
    {
        var airlines = await _repo.FindByIsActive(true);
        return airlines.Select(MapToAirlineDto).ToList();
    }

    // ── UPDATE AIRLINE ────────────────────────────────────────────────────────
    public async Task<AirlineResponseDto?> UpdateAirline(int airlineId, UpdateAirlineDto dto)
    {
        var airline = await _repo.FindByAirlineId(airlineId);
        if (airline == null) return null;

        airline.Name         = dto.Name.Trim();
        airline.IcaoCode     = dto.IcaoCode?.ToUpper().Trim();
        airline.LogoUrl      = dto.LogoUrl?.Trim();
        airline.Country      = dto.Country?.Trim();
        airline.ContactEmail = dto.ContactEmail?.Trim().ToLower();
        airline.ContactPhone = dto.ContactPhone?.Trim();
        airline.UpdatedAt    = DateTime.UtcNow;

        var updated = await _repo.UpdateAirline(airline);
        _logger.LogInformation("Airline updated: AirlineId={AirlineId}", airlineId);
        return MapToAirlineDto(updated);
    }

    // ── DEACTIVATE / REACTIVATE ───────────────────────────────────────────────
    public async Task DeactivateAirline(int airlineId)
    {
        var airline = await _repo.FindByAirlineId(airlineId)
            ?? throw new KeyNotFoundException($"Airline {airlineId} not found.");

        airline.IsActive  = false;
        airline.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAirline(airline);

        _logger.LogWarning("Airline deactivated: AirlineId={AirlineId} ({IataCode})",
            airlineId, airline.IataCode);
    }

    public async Task ReactivateAirline(int airlineId)
    {
        var airline = await _repo.FindByAirlineId(airlineId)
            ?? throw new KeyNotFoundException($"Airline {airlineId} not found.");

        airline.IsActive  = true;
        airline.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAirline(airline);

        _logger.LogInformation("Airline reactivated: AirlineId={AirlineId}", airlineId);
    }

    // ── CREATE AIRPORT ────────────────────────────────────────────────────────
    public async Task<AirportResponseDto> CreateAirport(CreateAirportDto dto)
    {
        dto.IataCode = dto.IataCode.ToUpper().Trim();

        if (await _repo.ExistsByAirportIataCode(dto.IataCode))
            throw new InvalidOperationException(
                $"An airport with IATA code '{dto.IataCode}' already exists.");

        var airport = new Airport
        {
            Name      = dto.Name.Trim(),
            IataCode  = dto.IataCode,
            IcaoCode  = dto.IcaoCode?.ToUpper().Trim(),
            City      = dto.City.Trim(),
            Country   = dto.Country.Trim(),
            Latitude  = dto.Latitude,
            Longitude = dto.Longitude,
            Timezone  = dto.Timezone?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var saved = await _repo.SaveAirport(airport);
        _logger.LogInformation("Airport created: {Name} ({IataCode}) — AirportId={AirportId}",
            saved.Name, saved.IataCode, saved.AirportId);

        return MapToAirportDto(saved);
    }

    // ── GET AIRPORT ───────────────────────────────────────────────────────────
    public async Task<AirportResponseDto?> GetAirportById(int airportId)
    {
        var airport = await _repo.FindAirportByAirportId(airportId);
        return airport == null ? null : MapToAirportDto(airport);
    }

    public async Task<AirportResponseDto?> GetAirportByIata(string iataCode)
    {
        var airport = await _repo.FindAirportByIataCode(iataCode.ToUpper());
        return airport == null ? null : MapToAirportDto(airport);
    }

    public async Task<IList<AirportResponseDto>> GetAllAirports()
    {
        var airports = await _repo.FindAllAirports();
        return airports.Select(MapToAirportDto).ToList();
    }

    public async Task<IList<AirportResponseDto>> GetAirportsByCity(string city)
    {
        var airports = await _repo.FindAirportsByCity(city);
        return airports.Select(MapToAirportDto).ToList();
    }

    public async Task<IList<AirportResponseDto>> SearchAirports(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<AirportResponseDto>();

        var airports = await _repo.SearchAirports(query.Trim(), maxResults: 10);
        return airports.Select(MapToAirportDto).ToList();
    }

    // ── UPDATE AIRPORT ────────────────────────────────────────────────────────
    public async Task<AirportResponseDto?> UpdateAirport(int airportId, UpdateAirportDto dto)
    {
        var airport = await _repo.FindAirportByAirportId(airportId);
        if (airport == null) return null;

        airport.Name      = dto.Name.Trim();
        airport.IcaoCode  = dto.IcaoCode?.ToUpper().Trim();
        airport.City      = dto.City.Trim();
        airport.Country   = dto.Country.Trim();
        airport.Latitude  = dto.Latitude;
        airport.Longitude = dto.Longitude;
        airport.Timezone  = dto.Timezone?.Trim();
        airport.UpdatedAt = DateTime.UtcNow;

        var updated = await _repo.UpdateAirport(airport);
        _logger.LogInformation("Airport updated: AirportId={AirportId}", airportId);
        return MapToAirportDto(updated);
    }

    // ── AIRLINE-AIRPORT LINKING ───────────────────────────────────────────────
    public async Task LinkAirportToAirline(int airlineId, int airportId)
    {
        // Validate both exist
        if (await _repo.FindByAirlineId(airlineId) == null)
            throw new KeyNotFoundException($"Airline {airlineId} not found.");
        if (await _repo.FindAirportByAirportId(airportId) == null)
            throw new KeyNotFoundException($"Airport {airportId} not found.");

        await _repo.LinkAirportToAirline(airlineId, airportId);
        _logger.LogInformation("Airport {AirportId} linked to Airline {AirlineId}", airportId, airlineId);
    }

    public async Task UnlinkAirportFromAirline(int airlineId, int airportId)
    {
        await _repo.UnlinkAirportFromAirline(airlineId, airportId);
        _logger.LogInformation("Airport {AirportId} unlinked from Airline {AirlineId}", airportId, airlineId);
    }

    public async Task<IList<AirportResponseDto>> GetAirportsByAirline(int airlineId)
    {
        var airports = await _repo.FindAirportsByAirlineId(airlineId);
        return airports.Select(MapToAirportDto).ToList();
    }

    // ── MAPPERS ───────────────────────────────────────────────────────────────
    public AirlineResponseDto MapToAirlineDto(Entities.Airline a) => new()
    {
        AirlineId    = a.AirlineId,
        Name         = a.Name,
        IataCode     = a.IataCode,
        IcaoCode     = a.IcaoCode,
        LogoUrl      = a.LogoUrl,
        Country      = a.Country,
        ContactEmail = a.ContactEmail,
        ContactPhone = a.ContactPhone,
        IsActive     = a.IsActive,
        CreatedAt    = a.CreatedAt
    };

    public AirportResponseDto MapToAirportDto(Airport a) => new()
    {
        AirportId = a.AirportId,
        Name      = a.Name,
        IataCode  = a.IataCode,
        IcaoCode  = a.IcaoCode,
        City      = a.City,
        Country   = a.Country,
        Latitude  = a.Latitude,
        Longitude = a.Longitude,
        Timezone  = a.Timezone,
        CreatedAt = a.CreatedAt
    };
}
