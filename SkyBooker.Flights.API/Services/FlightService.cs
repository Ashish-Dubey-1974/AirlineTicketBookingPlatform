using SkyBooker.Flights.API.DTOs;
using SkyBooker.Flights.API.Entities;
using SkyBooker.Flights.API.Repositories;

namespace SkyBooker.Flights.API.Services;

public class FlightService : IFlightService
{
    private readonly IFlightRepository _repo;
    private readonly ILogger<FlightService> _logger;

    private static readonly string[] ValidStatuses =
        { FlightStatus.Scheduled, FlightStatus.Delayed, FlightStatus.Cancelled,
          FlightStatus.Departed, FlightStatus.Arrived };

    public FlightService(IFlightRepository repo, ILogger<FlightService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────
    // ADD FLIGHT
    // ──────────────────────────────────────────────────────────────────
    public async Task<FlightResponse> AddFlightAsync(FlightCreateDto dto)
    {
        // Check FlightNumber uniqueness
        var existing = await _repo.GetByFlightNumberAsync(dto.FlightNumber);
        if (existing is not null)
            throw new InvalidOperationException($"Flight number '{dto.FlightNumber}' already exists.");

        // Calculate duration
        int duration = (int)(dto.ArrivalTime - dto.DepartureTime).TotalMinutes;
        if (duration <= 0)
            throw new ArgumentException("ArrivalTime must be after DepartureTime.");

        var flight = new Flight
        {
            FlightNumber        = dto.FlightNumber.ToUpper(),
            AirlineId           = dto.AirlineId,
            OriginAirportCode   = dto.OriginAirportCode.ToUpper(),
            DestinationAirportCode = dto.DestinationAirportCode.ToUpper(),
            DepartureTime       = dto.DepartureTime,
            ArrivalTime         = dto.ArrivalTime,
            DurationMinutes     = duration,
            AircraftType        = dto.AircraftType,
            TotalSeats          = dto.TotalSeats,
            AvailableSeats      = dto.TotalSeats,   // initially all seats available
            BasePrice           = dto.BasePrice,
            Status              = FlightStatus.Scheduled
        };

        var saved = await _repo.AddAsync(flight);
        _logger.LogInformation("Flight {FlightNumber} added with ID {FlightId}", saved.FlightNumber, saved.FlightId);
        return MapToResponse(saved);
    }

    // ──────────────────────────────────────────────────────────────────
    // GET BY ID / NUMBER
    // ──────────────────────────────────────────────────────────────────
    public async Task<FlightResponse?> GetFlightByIdAsync(int flightId)
    {
        var flight = await _repo.GetByIdAsync(flightId);
        return flight is null ? null : MapToResponse(flight);
    }

    public async Task<FlightResponse?> GetFlightByNumberAsync(string flightNumber)
    {
        var flight = await _repo.GetByFlightNumberAsync(flightNumber.ToUpper());
        return flight is null ? null : MapToResponse(flight);
    }

    // ──────────────────────────────────────────────────────────────────
    // UPDATE FLIGHT
    // ──────────────────────────────────────────────────────────────────
    public async Task<FlightResponse> UpdateFlightAsync(int flightId, FlightUpdateDto dto)
    {
        var flight = await _repo.GetByIdAsync(flightId)
            ?? throw new KeyNotFoundException($"Flight {flightId} not found.");

        if (dto.DepartureTime.HasValue) flight.DepartureTime = dto.DepartureTime.Value;
        if (dto.ArrivalTime.HasValue)   flight.ArrivalTime   = dto.ArrivalTime.Value;
        if (dto.AircraftType is not null) flight.AircraftType = dto.AircraftType;
        if (dto.BasePrice.HasValue)      flight.BasePrice     = dto.BasePrice.Value;

        // Recalculate duration if times changed
        flight.DurationMinutes = (int)(flight.ArrivalTime - flight.DepartureTime).TotalMinutes;

        // TotalSeats adjustment — only increase is safe; decreasing might be blocked if seats are booked
        if (dto.TotalSeats.HasValue && dto.TotalSeats.Value != flight.TotalSeats)
        {
            int diff = dto.TotalSeats.Value - flight.TotalSeats;
            flight.TotalSeats     = dto.TotalSeats.Value;
            flight.AvailableSeats = Math.Max(0, flight.AvailableSeats + diff);
        }

        await _repo.UpdateAsync(flight);
        _logger.LogInformation("Flight {FlightId} updated", flightId);
        return MapToResponse(flight);
    }

    // ──────────────────────────────────────────────────────────────────
    // UPDATE STATUS  (SCHEDULED → DELAYED → CANCELLED → DEPARTED → ARRIVED)
    // ──────────────────────────────────────────────────────────────────
    public async Task UpdateStatusAsync(int flightId, string status)
    {
        if (!ValidStatuses.Contains(status.ToUpper()))
            throw new ArgumentException($"Invalid status '{status}'. Allowed: {string.Join(", ", ValidStatuses)}");

        var flight = await _repo.GetByIdAsync(flightId)
            ?? throw new KeyNotFoundException($"Flight {flightId} not found.");

        flight.Status = status.ToUpper();
        await _repo.UpdateAsync(flight);
        _logger.LogInformation("Flight {FlightId} status updated to {Status}", flightId, status);
    }

    // ──────────────────────────────────────────────────────────────────
    // DELETE FLIGHT
    // ──────────────────────────────────────────────────────────────────
    public async Task DeleteFlightAsync(int flightId)
    {
        var flight = await _repo.GetByIdAsync(flightId)
            ?? throw new KeyNotFoundException($"Flight {flightId} not found.");

        // Guard: do not delete if there are confirmed bookings (checked by Booking service in production;
        // here we block if seats have been consumed as a proxy)
        if (flight.AvailableSeats < flight.TotalSeats)
            throw new InvalidOperationException("Cannot delete a flight that has active bookings.");

        await _repo.DeleteAsync(flight);
        _logger.LogInformation("Flight {FlightId} deleted", flightId);
    }

    // ──────────────────────────────────────────────────────────────────
    // SEARCH ONE-WAY
    // ──────────────────────────────────────────────────────────────────
    public async Task<IList<FlightResponse>> SearchFlightsAsync(FlightSearchRequest request)
    {
        var flights = await _repo.GetAvailableFlightsAsync(
            request.Origin.ToUpper(),
            request.Destination.ToUpper(),
            request.DepartureDate,
            request.Passengers);

        // Optional filters
        IEnumerable<Flight> result = flights;

        if (!string.IsNullOrWhiteSpace(request.Airline))
            result = result.Where(f => f.FlightNumber.StartsWith(request.Airline, StringComparison.OrdinalIgnoreCase));

        if (request.MaxPrice.HasValue)
            result = result.Where(f => f.BasePrice <= request.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(request.DepartureTimeRange))
        {
            result = request.DepartureTimeRange.ToLower() switch
            {
                "morning"   => result.Where(f => f.DepartureTime.Hour >= 6  && f.DepartureTime.Hour < 12),
                "afternoon" => result.Where(f => f.DepartureTime.Hour >= 12 && f.DepartureTime.Hour < 17),
                "evening"   => result.Where(f => f.DepartureTime.Hour >= 17 && f.DepartureTime.Hour < 21),
                "night"     => result.Where(f => f.DepartureTime.Hour >= 21 || f.DepartureTime.Hour < 6),
                _           => result
            };
        }

        return result.Select(MapToResponse).ToList();
    }

    // ──────────────────────────────────────────────────────────────────
    // SEARCH ROUND-TRIP
    // ──────────────────────────────────────────────────────────────────
    public async Task<Dictionary<string, IList<FlightResponse>>> SearchRoundTripAsync(RoundTripSearchRequest request)
    {
        // Outbound leg
        var outboundReq = new FlightSearchRequest
        {
            Origin        = request.Origin,
            Destination   = request.Destination,
            DepartureDate = request.DepartureDate,
            Passengers    = request.Passengers,
            Class         = request.Class
        };

        // Return leg
        var returnReq = new FlightSearchRequest
        {
            Origin        = request.Destination,
            Destination   = request.Origin,
            DepartureDate = request.ReturnDate,
            Passengers    = request.Passengers,
            Class         = request.Class
        };

        var outbound = await SearchFlightsAsync(outboundReq);
        var returnLeg = await SearchFlightsAsync(returnReq);

        return new Dictionary<string, IList<FlightResponse>>
        {
            ["outbound"] = outbound,
            ["return"]   = returnLeg
        };
    }

    // ──────────────────────────────────────────────────────────────────
    // AIRLINE STAFF — all flights for an airline
    // ──────────────────────────────────────────────────────────────────
    public async Task<IList<FlightResponse>> GetFlightsByAirlineAsync(int airlineId)
    {
        var flights = await _repo.GetByAirlineIdAsync(airlineId);
        return flights.Select(MapToResponse).ToList();
    }

    // ──────────────────────────────────────────────────────────────────
    // ATOMIC SEAT COUNTERS
    // ──────────────────────────────────────────────────────────────────
    public async Task<bool> DecrementSeatsAsync(int flightId, int count)
    {
        var rowsAffected = await _repo.DecrementSeatsAsync(flightId, count);
        if (rowsAffected == 0)
        {
            _logger.LogWarning("DecrementSeats failed for Flight {FlightId} — insufficient seats or flight not found", flightId);
            return false;
        }
        return true;
    }

    public async Task IncrementSeatsAsync(int flightId, int count)
    {
        await _repo.IncrementSeatsAsync(flightId, count);
    }

    // ──────────────────────────────────────────────────────────────────
    // MAPPER  (Flight entity → FlightResponse DTO)
    // ──────────────────────────────────────────────────────────────────
    private static FlightResponse MapToResponse(Flight f)
    {
        int hours   = f.DurationMinutes / 60;
        int minutes = f.DurationMinutes % 60;

        return new FlightResponse
        {
            FlightId               = f.FlightId,
            FlightNumber           = f.FlightNumber,
            AirlineId              = f.AirlineId,
            OriginAirportCode      = f.OriginAirportCode,
            DestinationAirportCode = f.DestinationAirportCode,
            DepartureTime          = f.DepartureTime,
            ArrivalTime            = f.ArrivalTime,
            DurationMinutes        = f.DurationMinutes,
            DurationDisplay        = $"{hours}h {minutes}m",
            Status                 = f.Status,
            AircraftType           = f.AircraftType,
            AvailableSeats         = f.AvailableSeats,
            BasePrice              = f.BasePrice,
            // Fare class pricing multipliers (per plan spec)
            FareClasses = new Dictionary<string, FareClassInfo>
            {
                ["Economy"] = new FareClassInfo
                {
                    Price                  = f.BasePrice,
                    BaggageAllowance       = 15,
                    IsRefundable           = false,
                    CancellationFeePercent = 30
                },
                ["Business"] = new FareClassInfo
                {
                    Price                  = Math.Round(f.BasePrice * 2.5m, 2),
                    BaggageAllowance       = 30,
                    IsRefundable           = true,
                    CancellationFeePercent = 10
                },
                ["First"] = new FareClassInfo
                {
                    Price                  = Math.Round(f.BasePrice * 4.5m, 2),
                    BaggageAllowance       = 40,
                    IsRefundable           = true,
                    CancellationFeePercent = 0
                }
            }
        };
    }
}
