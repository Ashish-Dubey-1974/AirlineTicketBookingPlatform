using SkyBooker.Flights.API.DTOs;
using SkyBooker.Flights.API.Entities;

namespace SkyBooker.Flights.API.Services;

public interface IFlightService
{
    // CRUD
    Task<FlightResponse> AddFlightAsync(FlightCreateDto dto);
    Task<FlightResponse?> GetFlightByIdAsync(int flightId);
    Task<FlightResponse?> GetFlightByNumberAsync(string flightNumber);
    Task<FlightResponse> UpdateFlightAsync(int flightId, FlightUpdateDto dto);
    Task UpdateStatusAsync(int flightId, string status);
    Task DeleteFlightAsync(int flightId);

    // Search
    Task<IList<FlightResponse>> SearchFlightsAsync(FlightSearchRequest request);
    Task<Dictionary<string, IList<FlightResponse>>> SearchRoundTripAsync(RoundTripSearchRequest request);

    // Airline staff
    Task<IList<FlightResponse>> GetFlightsByAirlineAsync(int airlineId);

    // Seat counter (called by Booking service)
    Task<bool> DecrementSeatsAsync(int flightId, int count);
    Task IncrementSeatsAsync(int flightId, int count);
}
