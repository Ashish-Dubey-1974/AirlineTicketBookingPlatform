using Microsoft.EntityFrameworkCore;
using SkyBooker.Airline.Data;
using SkyBooker.Airline.Entities;

namespace SkyBooker.Airline.Repositories;

/// <summary>
/// EF Core implementation of IAirlineRepository.
/// Uses AirlineDbContext for all database operations.
/// Registered as Scoped in Program.cs DI container.
/// </summary>
public class AirlineRepository : IAirlineRepository
{
    private readonly AirlineDbContext _context;
    private readonly ILogger<AirlineRepository> _logger;

    public AirlineRepository(AirlineDbContext context, ILogger<AirlineRepository> logger)
    {
        _context = context;
        _logger  = logger;
    }

    // ── AIRLINE ───────────────────────────────────────────────────────────────

    public async Task<Entities.Airline?> FindByAirlineId(int airlineId)
        => await _context.Airlines
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AirlineId == airlineId);

    public async Task<Entities.Airline?> FindByIataCode(string iataCode)
        => await _context.Airlines
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.IataCode == iataCode.ToUpper());

    public async Task<IList<Entities.Airline>> FindAllAirlines()
        => await _context.Airlines
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync();

    public async Task<IList<Entities.Airline>> FindByIsActive(bool isActive)
        => await _context.Airlines
            .AsNoTracking()
            .Where(a => a.IsActive == isActive)
            .OrderBy(a => a.Name)
            .ToListAsync();

    public async Task<bool> ExistsByAirlineIataCode(string iataCode)
        => await _context.Airlines
            .AnyAsync(a => a.IataCode == iataCode.ToUpper());

    public async Task<Entities.Airline> SaveAirline(Entities.Airline airline)
    {
        _context.Airlines.Add(airline);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Airline saved: {Name} ({IataCode})", airline.Name, airline.IataCode);
        return airline;
    }

    public async Task<Entities.Airline> UpdateAirline(Entities.Airline airline)
    {
        var tracked = await _context.Airlines.FindAsync(airline.AirlineId)
            ?? throw new KeyNotFoundException($"Airline {airline.AirlineId} not found.");

        _context.Entry(tracked).CurrentValues.SetValues(airline);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Airline updated: AirlineId={AirlineId}", airline.AirlineId);
        return tracked;
    }

    // ── AIRPORT ───────────────────────────────────────────────────────────────

    public async Task<Airport?> FindAirportByAirportId(int airportId)
        => await _context.Airports
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AirportId == airportId);

    public async Task<Airport?> FindAirportByIataCode(string iataCode)
        => await _context.Airports
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.IataCode == iataCode.ToUpper());

    public async Task<IList<Airport>> FindAllAirports()
        => await _context.Airports
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync();

    public async Task<IList<Airport>> FindAirportsByCity(string city)
        => await _context.Airports
            .AsNoTracking()
            .Where(a => EF.Functions.Like(a.City, $"%{city}%"))
            .OrderBy(a => a.City)
            .ToListAsync();

    public async Task<IList<Airport>> FindAirportsByCountry(string country)
        => await _context.Airports
            .AsNoTracking()
            .Where(a => a.Country.ToLower() == country.ToLower())
            .OrderBy(a => a.Name)
            .ToListAsync();

    public async Task<bool> ExistsByAirportIataCode(string iataCode)
        => await _context.Airports
            .AnyAsync(a => a.IataCode == iataCode.ToUpper());

    public async Task<Airport> SaveAirport(Airport airport)
    {
        _context.Airports.Add(airport);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Airport saved: {Name} ({IataCode})", airport.Name, airport.IataCode);
        return airport;
    }

    public async Task<Airport> UpdateAirport(Airport airport)
    {
        var tracked = await _context.Airports.FindAsync(airport.AirportId)
            ?? throw new KeyNotFoundException($"Airport {airport.AirportId} not found.");

        _context.Entry(tracked).CurrentValues.SetValues(airport);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Airport updated: AirportId={AirportId}", airport.AirportId);
        return tracked;
    }

    /// <summary>
    /// EF.Functions.Like search on Name OR IataCode for autocomplete.
    /// Minimum 2-char query recommended to avoid full-table scan.
    /// </summary>
    public async Task<IList<Airport>> SearchAirports(string query, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<Airport>();

        var pattern = $"%{query.Trim()}%";

        return await _context.Airports
            .AsNoTracking()
            .Where(a =>
                EF.Functions.Like(a.Name,     pattern) ||
                EF.Functions.Like(a.IataCode, pattern) ||
                EF.Functions.Like(a.City,     pattern))
            .OrderBy(a => a.IataCode)
            .Take(maxResults)
            .ToListAsync();
    }

    // ── AIRLINE-AIRPORT MANY-TO-MANY ──────────────────────────────────────────

    public async Task LinkAirportToAirline(int airlineId, int airportId)
    {
        var exists = await _context.AirlineAirports
            .AnyAsync(aa => aa.AirlineId == airlineId && aa.AirportId == airportId);

        if (!exists)
        {
            _context.AirlineAirports.Add(new AirlineAirport
            {
                AirlineId = airlineId,
                AirportId = airportId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }

    public async Task UnlinkAirportFromAirline(int airlineId, int airportId)
    {
        var link = await _context.AirlineAirports
            .FirstOrDefaultAsync(aa => aa.AirlineId == airlineId && aa.AirportId == airportId);

        if (link != null)
        {
            _context.AirlineAirports.Remove(link);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IList<Airport>> FindAirportsByAirlineId(int airlineId)
        => await _context.AirlineAirports
            .AsNoTracking()
            .Where(aa => aa.AirlineId == airlineId)
            .Include(aa => aa.Airport)
            .Select(aa => aa.Airport)
            .OrderBy(a => a.Name)
            .ToListAsync();
}
