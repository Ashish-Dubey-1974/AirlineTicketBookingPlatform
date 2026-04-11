using SkyBooker.Flights.API.Entities;

namespace SkyBooker.Flights.API.Repositories;

public interface IFlightRepository
{
    Task<Flight?> GetByIdAsync(int id);
    Task<Flight?> GetByFlightNumberAsync(string flightNumber);
    Task<IEnumerable<Flight>> GetByOriginDestDateAsync(string origin, string dest, DateTime date);
    Task<IEnumerable<Flight>> GetByAirlineIdAsync(int airlineId);
    Task<IEnumerable<Flight>> GetByStatusAsync(string status);
    Task<IEnumerable<Flight>> GetAvailableFlightsAsync(string origin, string dest, DateTime date, int passengers);
    Task<int> CountByAirlineIdAsync(int airlineId);
    Task<Flight> AddAsync(Flight flight);
    Task UpdateAsync(Flight flight);
    Task DeleteAsync(Flight flight);
    Task<int> DecrementSeatsAsync(int flightId, int count);
    Task<int> IncrementSeatsAsync(int flightId, int count);
}