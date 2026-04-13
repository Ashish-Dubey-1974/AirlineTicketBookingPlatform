using FluentValidation;
using SkyBooker.Passenger.API.DTOs;
using SkyBooker.Passenger.API.Entities;
using SkyBooker.Passenger.API.Repositories;
using SkyBooker.Passenger.API.Validators;

namespace SkyBooker.Passenger.API.Services;

public class PassengerService : IPassengerService
{
    private readonly IPassengerRepository _repo;
    private readonly IValidator<PassengerRequestDto> _validator;
    private readonly ILogger<PassengerService> _logger;
    private static readonly Random _random = new();

    public PassengerService(
        IPassengerRepository repo, 
        IValidator<PassengerRequestDto> validator,
        ILogger<PassengerService> logger)
    {
        _repo = repo;
        _validator = validator;
        _logger = logger;
    }

    public async Task<PassengerResponseDto> AddPassengerAsync(PassengerRequestDto dto)
    {
        // Validate using FluentValidation
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var passenger = new PassengerInfo
        {
            BookingId = dto.BookingId,
            Title = dto.Title,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            PassportNumber = dto.PassportNumber.ToUpper().Trim(),
            Nationality = dto.Nationality.Trim(),
            PassportExpiry = dto.PassportExpiry,
            SeatId = dto.SeatId,
            SeatNumber = dto.SeatNumber,
            PassengerType = dto.PassengerType,
            CreatedAt = DateTime.UtcNow
        };

        var saved = await _repo.AddAsync(passenger);
        return MapToResponse(saved);
    }

    public async Task<PassengerResponseDto?> GetPassengerByIdAsync(int passengerId)
    {
        var passenger = await _repo.GetByIdAsync(passengerId);
        return passenger == null ? null : MapToResponse(passenger);
    }

    public async Task<IList<PassengerResponseDto>> GetPassengersByBookingAsync(string bookingId)
    {
        var passengers = await _repo.GetByBookingIdAsync(bookingId);
        return passengers.Select(MapToResponse).ToList();
    }

    public async Task<PassengerResponseDto?> GetByPassportNumberAsync(string passportNumber)
    {
        var passenger = await _repo.GetByPassportNumberAsync(passportNumber);
        return passenger == null ? null : MapToResponse(passenger);
    }

    public async Task<PassengerResponseDto> UpdatePassengerAsync(int passengerId, PassengerRequestDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var passenger = await _repo.GetByIdAsync(passengerId);
        if (passenger == null)
            throw new KeyNotFoundException($"Passenger {passengerId} not found");

        passenger.Title = dto.Title;
        passenger.FirstName = dto.FirstName.Trim();
        passenger.LastName = dto.LastName.Trim();
        passenger.DateOfBirth = dto.DateOfBirth;
        passenger.Gender = dto.Gender;
        passenger.PassportNumber = dto.PassportNumber.ToUpper().Trim();
        passenger.Nationality = dto.Nationality.Trim();
        passenger.PassportExpiry = dto.PassportExpiry;
        passenger.PassengerType = dto.PassengerType;

        var updated = await _repo.UpdateAsync(passenger);
        return MapToResponse(updated);
    }

    public async Task<PassengerResponseDto> AssignSeatAsync(int passengerId, int seatId, string seatNumber)
    {
        var passenger = await _repo.GetByIdAsync(passengerId);
        if (passenger == null)
            throw new KeyNotFoundException($"Passenger {passengerId} not found");

        passenger.SeatId = seatId;
        passenger.SeatNumber = seatNumber;
        passenger.UpdatedAt = DateTime.UtcNow;

        var updated = await _repo.UpdateAsync(passenger);
        _logger.LogInformation("Seat {SeatNumber} assigned to Passenger {PassengerId}", seatNumber, passengerId);
        return MapToResponse(updated);
    }

    public async Task<string> GenerateTicketNumberAsync(string airlineCode, string flightNumber)
    {
        // Format: {AirlineCode}{FlightNumber}-{Random6Digits}
        // Example: AI202-847293
        var random6 = _random.Next(100000, 999999).ToString();
        var ticketNumber = $"{airlineCode}{flightNumber}-{random6}";
        
        // Ensure uniqueness
        var existing = await _repo.GetByTicketNumberAsync(ticketNumber);
        if (existing != null)
        {
            return await GenerateTicketNumberAsync(airlineCode, flightNumber);
        }
        
        return ticketNumber;
    }

    public async Task<PassengerResponseDto> WebCheckInAsync(int passengerId, int newSeatId, string newSeatNumber)
    {
        var passenger = await _repo.GetByIdAsync(passengerId);
        if (passenger == null)
            throw new KeyNotFoundException($"Passenger {passengerId} not found");

        // Check if within 24h-1h window (server-side validation)
        // This would need flight departure time from Flight API
        
        passenger.CheckedIn = true;
        passenger.CheckedInAt = DateTime.UtcNow;
        passenger.SeatId = newSeatId;
        passenger.SeatNumber = newSeatNumber;
        passenger.UpdatedAt = DateTime.UtcNow;

        // Generate ticket number if not exists
        if (string.IsNullOrEmpty(passenger.TicketNumber))
        {
            passenger.TicketNumber = await GenerateTicketNumberAsync("SKY", "FLT");
        }

        var updated = await _repo.UpdateAsync(passenger);
        _logger.LogInformation("Web check-in completed for Passenger {PassengerId}", passengerId);
        return MapToResponse(updated);
    }

    public async Task<bool> DeleteByBookingAsync(string bookingId)
    {
        return await _repo.DeleteByBookingIdAsync(bookingId);
    }

    public async Task<int> GetPassengerCountByBookingAsync(string bookingId)
    {
        return await _repo.CountByBookingIdAsync(bookingId);
    }

    private static PassengerResponseDto MapToResponse(PassengerInfo p) => new()
    {
        PassengerId = p.PassengerId,
        BookingId = p.BookingId,
        Title = p.Title,
        FirstName = p.FirstName,
        LastName = p.LastName,
        DateOfBirth = p.DateOfBirth,
        Gender = p.Gender,
        PassportNumber = p.PassportNumber,
        Nationality = p.Nationality,
        PassportExpiry = p.PassportExpiry,
        SeatId = p.SeatId,
        SeatNumber = p.SeatNumber,
        TicketNumber = p.TicketNumber,
        PassengerType = p.PassengerType,
        CheckedIn = p.CheckedIn,
        CheckedInAt = p.CheckedInAt,
        CreatedAt = p.CreatedAt
    };
}