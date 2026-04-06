using SkyBooker.Airline.Entities;

namespace SkyBooker.Airline.Repositories;

/// <summary>
/// Repository interface for Airline and Airport data access.
/// Implemented by AirlineRepository using AirlineDbContext.
/// Registered as Scoped in Program.cs.
/// </summary>
public interface IAirlineRepository
{
    // ── Airline CRUD ──────────────────────────────────────────────────────────

    Task<Entities.Airline?> FindByAirlineId(int airlineId);
    Task<Entities.Airline?> FindByIataCode(string iataCode);
    Task<IList<Entities.Airline>> FindAllAirlines();
    Task<IList<Entities.Airline>> FindByIsActive(bool isActive);
    Task<Entities.Airline> SaveAirline(Entities.Airline airline);
    Task<Entities.Airline> UpdateAirline(Entities.Airline airline);
    Task<bool> ExistsByAirlineIataCode(string iataCode);

    // ── Airport CRUD ──────────────────────────────────────────────────────────

    Task<Airport?> FindAirportByAirportId(int airportId);
    Task<Airport?> FindAirportByIataCode(string iataCode);
    Task<IList<Airport>> FindAllAirports();
    Task<IList<Airport>> FindAirportsByCity(string city);
    Task<IList<Airport>> FindAirportsByCountry(string country);
    Task<Airport> SaveAirport(Airport airport);
    Task<Airport> UpdateAirport(Airport airport);
    Task<bool> ExistsByAirportIataCode(string iataCode);

    /// <summary>
    /// EF.Functions.Like search on airport Name OR IataCode for autocomplete.
    /// Returns up to <paramref name="maxResults"/> results.
    /// Query must be at least 2 characters.
    /// </summary>
    Task<IList<Airport>> SearchAirports(string query, int maxResults = 10);

    // ── AirlineAirport many-to-many ───────────────────────────────────────────

    Task LinkAirportToAirline(int airlineId, int airportId);
    Task UnlinkAirportFromAirline(int airlineId, int airportId);
    Task<IList<Airport>> FindAirportsByAirlineId(int airlineId);
}
