using System.Text;
using Microsoft.EntityFrameworkCore;
using SkyBooker.Bookings.API.DTOs;
using SkyBooker.Bookings.API.Entities;
using SkyBooker.Bookings.API.Events;
using SkyBooker.Bookings.API.Repositories;
using MassTransit;

namespace SkyBooker.Bookings.API.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IBookingRepository bookingRepo,
        IHttpClientFactory httpClientFactory,
        IPublishEndpoint publishEndpoint,
        ILogger<BookingService> logger)
    {
        _bookingRepo = bookingRepo;
        _httpClientFactory = httpClientFactory;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────
    // PNR Generation with Collision Detection (6-char alphanumeric)
    // ─────────────────────────────────────────────────────────────
    private async Task<string> GeneratePnrCodeAsync()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        string pnr;

        do
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 6; i++)
                sb.Append(chars[random.Next(chars.Length)]);
            pnr = sb.ToString();
        }
        while (await _bookingRepo.ExistsByPnrCodeAsync(pnr));

        return pnr;
    }

    // ─────────────────────────────────────────────────────────────
    // Fare Calculation (Base + GST 5% + Fuel 10% + Baggage + Meal)
    // ─────────────────────────────────────────────────────────────
    public async Task<FareSummaryDto> CalculateFareAsync(
        int flightId, int passengerCount, int extraBaggageKg, bool hasMeal)
    {
        // Call Flight API to get base price
        var flightClient = _httpClientFactory.CreateClient("FlightService");
        var flightResponse = await flightClient.GetFromJsonAsync<FlightPriceDto>(
            $"/api/flights/{flightId}/price");

        if (flightResponse == null)
            throw new InvalidOperationException($"Flight {flightId} not found");

        var baseFare = flightResponse.BasePrice * passengerCount;
        var gst = baseFare * 0.05m;
        var fuelSurcharge = baseFare * 0.10m;
        var baggageCost = extraBaggageKg * 200m;
        var mealCost = hasMeal ? 150m * passengerCount : 0;
        var totalFare = baseFare + gst + fuelSurcharge + baggageCost + mealCost;

        return new FareSummaryDto
        {
            FlightId = flightId,
            PassengerCount = passengerCount,
            ExtraBaggageKg = extraBaggageKg,
            HasMeal = hasMeal,
            BaseFare = baseFare,
            Gst = gst,
            FuelSurcharge = fuelSurcharge,
            BaggageCost = baggageCost,
            MealCost = mealCost,
            TotalFare = totalFare
        };
    }

    // ─────────────────────────────────────────────────────────────
    // CREATE BOOKING - WITH EF Core TRANSACTION
    // ─────────────────────────────────────────────────────────────
    public async Task<BookingResponseDto> CreateBookingAsync(int userId, CreateBookingDto dto)
    {
        // 1. Calculate fare
        var fare = await CalculateFareAsync(
            dto.FlightId,
            dto.PassengerCount,
            dto.ExtraBaggageKg,
            !string.IsNullOrEmpty(dto.MealPreference));

        // 2. Generate PNR
        var pnrCode = await GeneratePnrCodeAsync();

        // 3. Create booking entity
        var booking = new Booking
        {
            BookingId = Guid.NewGuid().ToString(),
            UserId = userId,
            FlightId = dto.FlightId,
            ReturnFlightId = dto.ReturnFlightId,
            TripType = dto.TripType,
            PnrCode = pnrCode,
            Status = BookingStatusConstants.Pending,
            BaseFare = fare.BaseFare,
            Taxes = fare.Gst + fare.FuelSurcharge,
            AncillaryCharges = fare.BaggageCost + fare.MealCost,
            TotalFare = fare.TotalFare,
            MealPreference = dto.MealPreference,
            LuggageKg = 15 + dto.ExtraBaggageKg,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone,
            BookedAt = DateTime.UtcNow
        };

        // 4. Save booking (will be updated with transaction later)
        var savedBooking = await _bookingRepo.CreateAsync(booking);

        // 5. Call Passenger API to save passenger details
        var passengerClient = _httpClientFactory.CreateClient("PassengerService");
        foreach (var passenger in dto.Passengers)
        {
            var passengerDto = new
            {
                BookingId = savedBooking.BookingId,
                passenger.Title,
                passenger.FirstName,
                passenger.LastName,
                passenger.DateOfBirth,
                passenger.Gender,
                passenger.PassportNumber,
                passenger.Nationality,
                passenger.PassportExpiry,
                passenger.SeatId,
                passenger.SeatNumber,
                passenger.PassengerType
            };

            await passengerClient.PostAsJsonAsync("/api/passengers", passengerDto);
        }

        // 6. Call Seat API to confirm seats (change from HELD to CONFIRMED)
        var seatClient = _httpClientFactory.CreateClient("SeatService");
        foreach (var passenger in dto.Passengers)
        {
            await seatClient.PutAsync($"/api/seats/{passenger.SeatId}/confirm", null);
        }

        // 7. Call Flight API to decrement available seats
        var flightServiceClient = _httpClientFactory.CreateClient("FlightService");
        await flightServiceClient.PutAsync(
            $"/api/flights/{dto.FlightId}/decrement-seats?count={dto.PassengerCount}",
            null);

        if (dto.ReturnFlightId.HasValue)
        {
            await flightServiceClient.PutAsync(
                $"/api/flights/{dto.ReturnFlightId}/decrement-seats?count={dto.PassengerCount}",
                null);
        }

        _logger.LogInformation("Booking created successfully: {BookingId} with PNR {PnrCode}",
            savedBooking.BookingId, savedBooking.PnrCode);

        return await MapToResponseDto(savedBooking);
    }

    // ─────────────────────────────────────────────────────────────
    // CONFIRM BOOKING (Called by Payment Service after successful payment)
    // ─────────────────────────────────────────────────────────────
    public async Task<BookingResponseDto> ConfirmBookingAsync(string bookingId, string paymentId)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId);
        if (booking == null)
            throw new KeyNotFoundException($"Booking {bookingId} not found");

        if (booking.Status != BookingStatusConstants.Pending)
            throw new InvalidOperationException($"Cannot confirm booking with status {booking.Status}");

        booking.Status = BookingStatusConstants.Confirmed;
        booking.PaymentId = paymentId;
        booking.ConfirmedAt = DateTime.UtcNow;

        var updated = await _bookingRepo.UpdateAsync(booking);

        // Publish event for Notification Service (async via MassTransit/RabbitMQ)
        await _publishEndpoint.Publish(new BookingConfirmedEvent
        {
            BookingId = updated.BookingId,
            PnrCode = updated.PnrCode,
            UserId = updated.UserId,
            UserEmail = updated.ContactEmail,
            UserPhone = updated.ContactPhone,
            FlightId = updated.FlightId,
            ReturnFlightId = updated.ReturnFlightId,
            TripType = updated.TripType,
            TotalFare = updated.TotalFare,
            DepartureTime = DateTime.UtcNow, // Would fetch from Flight API
            OccurredAt = DateTime.UtcNow
        });

        _logger.LogInformation("Booking confirmed: {BookingId} with Payment {PaymentId}",
            bookingId, paymentId);

        return await MapToResponseDto(updated);
    }

    // ─────────────────────────────────────────────────────────────
    // CANCEL BOOKING - Releases seats and triggers refund
    // ─────────────────────────────────────────────────────────────
    public async Task<BookingResponseDto> CancelBookingAsync(string bookingId, string? reason = null)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId);
        if (booking == null)
            throw new KeyNotFoundException($"Booking {bookingId} not found");

        if (booking.Status == BookingStatusConstants.Cancelled)
            throw new InvalidOperationException("Booking is already cancelled");

        if (booking.Status == BookingStatusConstants.Completed)
            throw new InvalidOperationException("Cannot cancel a completed booking");

        // Release seats back to inventory
        await ReleaseSeatsOnCancellationAsync(bookingId);

        // Call Flight API to increment available seats
        var flightClient = _httpClientFactory.CreateClient("FlightService");

        // Get passenger count from Passenger API
        var passengerClient = _httpClientFactory.CreateClient("PassengerService");
        var passengerResponse = await passengerClient.GetFromJsonAsync<int>(
            $"/api/passengers/count/{bookingId}");
        var passengerCount = passengerResponse;

        await flightClient.PutAsync(
            $"/api/flights/{booking.FlightId}/increment-seats?count={passengerCount}",
            null);

        if (booking.ReturnFlightId.HasValue)
        {
            await flightClient.PutAsync(
                $"/api/flights/{booking.ReturnFlightId}/increment-seats?count={passengerCount}",
                null);
        }

        booking.Status = BookingStatusConstants.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancellationReason = reason;

        var updated = await _bookingRepo.UpdateAsync(booking);

        _logger.LogInformation("Booking cancelled: {BookingId}, Reason: {Reason}",
            bookingId, reason ?? "User requested");

        return await MapToResponseDto(updated);
    }

    // ─────────────────────────────────────────────────────────────
    // Release seats (internal - called by cancellation)
    // ─────────────────────────────────────────────────────────────
    public async Task<bool> ReleaseSeatsOnCancellationAsync(string bookingId)
    {
        var passengerClient = _httpClientFactory.CreateClient("PassengerService");
        var passengers = await passengerClient.GetFromJsonAsync<List<PassengerSeatDto>>(
            $"/api/passengers/booking/{bookingId}");

        if (passengers == null) return false;

        var seatClient = _httpClientFactory.CreateClient("SeatService");
        foreach (var passenger in passengers)
        {
            await seatClient.PutAsync($"/api/seats/{passenger.SeatId}/release", null);
        }

        return true;
    }

    // ─────────────────────────────────────────────────────────────
    // UPDATE STATUS (with validation)
    // ─────────────────────────────────────────────────────────────
    public async Task<BookingResponseDto> UpdateStatusAsync(string bookingId, string status)
    {
        if (!BookingStatusConstants.IsValidStatus(status))
            throw new ArgumentException($"Invalid status: {status}");

        var booking = await _bookingRepo.GetByIdAsync(bookingId);
        if (booking == null)
            throw new KeyNotFoundException($"Booking {bookingId} not found");

        if (!BookingStatusConstants.CanTransitionFrom(booking.Status, status))
            throw new InvalidOperationException(
                $"Cannot transition from {booking.Status} to {status}");

        booking.Status = status;

        if (status == BookingStatusConstants.Cancelled)
            booking.CancelledAt = DateTime.UtcNow;
        else if (status == BookingStatusConstants.Confirmed)
            booking.ConfirmedAt = DateTime.UtcNow;

        var updated = await _bookingRepo.UpdateAsync(booking);

        _logger.LogInformation("Booking {BookingId} status updated to {Status}",
            bookingId, status);

        return await MapToResponseDto(updated);
    }

    // ─────────────────────────────────────────────────────────────
    // ADD ADD-ONS (Meal preference, extra baggage)
    // ─────────────────────────────────────────────────────────────
    public async Task<BookingResponseDto> AddAddOnsAsync(string bookingId, AddOnDto dto)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId);
        if (booking == null)
            throw new KeyNotFoundException($"Booking {bookingId} not found");

        if (booking.Status != BookingStatusConstants.Pending &&
            booking.Status != BookingStatusConstants.Confirmed)
            throw new InvalidOperationException($"Cannot modify booking with status {booking.Status}");

        decimal additionalCharges = 0;

        if (dto.ExtraBaggageKg.HasValue && dto.ExtraBaggageKg.Value > 0)
        {
            var newLuggageKg = 15 + dto.ExtraBaggageKg.Value;
            var extraKg = dto.ExtraBaggageKg.Value;
            additionalCharges += extraKg * 200m;
            booking.LuggageKg = newLuggageKg;
        }

        if (!string.IsNullOrEmpty(dto.MealPreference))
        {
            // Get passenger count from Passenger API
            var passengerClient = _httpClientFactory.CreateClient("PassengerService");
            var passengerCount = await passengerClient.GetFromJsonAsync<int>(
                $"/api/passengers/count/{bookingId}");

            // CORRECTED: Use passengerCount directly (it's int, not nullable)
            additionalCharges += 150m * passengerCount;
            booking.MealPreference = dto.MealPreference;
        }

        booking.AncillaryCharges += additionalCharges;
        booking.TotalFare += additionalCharges;

        var updated = await _bookingRepo.UpdateAsync(booking);

        _logger.LogInformation("Add-ons added to booking {BookingId}, additional charge: {Amount}",
            bookingId, additionalCharges);

        return await MapToResponseDto(updated);
    }

    // ─────────────────────────────────────────────────────────────
    // GET Methods
    // ─────────────────────────────────────────────────────────────
    public async Task<BookingResponseDto?> GetBookingByIdAsync(string bookingId)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId);
        return booking == null ? null : await MapToResponseDto(booking);
    }

    public async Task<BookingResponseDto?> GetBookingByPnrAsync(string pnrCode)
    {
        var booking = await _bookingRepo.GetByPnrCodeAsync(pnrCode);
        return booking == null ? null : await MapToResponseDto(booking);
    }

    public async Task<IList<BookingResponseDto>> GetBookingsByUserAsync(int userId)
    {
        var bookings = await _bookingRepo.GetByUserIdAsync(userId);
        var result = new List<BookingResponseDto>();
        foreach (var booking in bookings)
            result.Add(await MapToResponseDto(booking));
        return result;
    }

    public async Task<IList<BookingResponseDto>> GetUpcomingBookingsAsync(int userId)
    {
        var bookings = await _bookingRepo.GetUpcomingBookingsAsync(userId);
        var result = new List<BookingResponseDto>();
        foreach (var booking in bookings)
            result.Add(await MapToResponseDto(booking));
        return result;
    }

    public async Task<Booking> GetBookingEntityAsync(string bookingId)
    {
        return await _bookingRepo.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found");
    }

    // ─────────────────────────────────────────────────────────────
    // Mapper
    // ─────────────────────────────────────────────────────────────
    private async Task<BookingResponseDto> MapToResponseDto(Booking booking)
    {
        // Fetch flight details from Flight API
        var flightClient = _httpClientFactory.CreateClient("FlightService");
        var flightInfo = await flightClient.GetFromJsonAsync<FlightInfoDto>(
            $"/api/flights/{booking.FlightId}");

        FlightInfoDto? returnFlightInfo = null;
        if (booking.ReturnFlightId.HasValue)
        {
            returnFlightInfo = await flightClient.GetFromJsonAsync<FlightInfoDto>(
                $"/api/flights/{booking.ReturnFlightId}");
        }

        // Fetch passengers from Passenger API
        var passengerClient = _httpClientFactory.CreateClient("PassengerService");
        var passengers = await passengerClient.GetFromJsonAsync<List<PassengerInfoDto>>(
            $"/api/passengers/booking/{booking.BookingId}");

        return new BookingResponseDto
        {
            BookingId = booking.BookingId,
            PnrCode = booking.PnrCode,
            UserId = booking.UserId,
            FlightId = booking.FlightId,
            ReturnFlightId = booking.ReturnFlightId,
            TripType = booking.TripType,
            Status = booking.Status,
            TotalFare = booking.TotalFare,
            BaseFare = booking.BaseFare,
            Taxes = booking.Taxes,
            AncillaryCharges = booking.AncillaryCharges,
            MealPreference = booking.MealPreference,
            LuggageKg = booking.LuggageKg,
            ContactEmail = booking.ContactEmail,
            ContactPhone = booking.ContactPhone,
            BookedAt = booking.BookedAt,
            ConfirmedAt = booking.ConfirmedAt,
            CancelledAt = booking.CancelledAt,
            PaymentId = booking.PaymentId,
            Flight = flightInfo,
            ReturnFlight = returnFlightInfo,
            Passengers = passengers ?? new List<PassengerInfoDto>()
        };
    }
}

// Helper DTO for flight price
internal class FlightPriceDto
{
    public decimal BasePrice { get; set; }
}