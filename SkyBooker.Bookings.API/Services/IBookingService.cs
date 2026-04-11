using SkyBooker.Bookings.API.DTOs;
using SkyBooker.Bookings.API.Entities;

namespace SkyBooker.Bookings.API.Services;

public interface IBookingService
{
    // Core Booking Operations
    Task<BookingResponseDto> CreateBookingAsync(int userId, CreateBookingDto dto);
    Task<BookingResponseDto?> GetBookingByIdAsync(string bookingId);
    Task<BookingResponseDto?> GetBookingByPnrAsync(string pnrCode);
    Task<IList<BookingResponseDto>> GetBookingsByUserAsync(int userId);
    Task<IList<BookingResponseDto>> GetUpcomingBookingsAsync(int userId);
    
    // Booking Lifecycle
    Task<BookingResponseDto> CancelBookingAsync(string bookingId, string? reason = null);
    Task<BookingResponseDto> UpdateStatusAsync(string bookingId, string status);
    Task<BookingResponseDto> ConfirmBookingAsync(string bookingId, string paymentId);
    
    // Fare Calculation
    Task<FareSummaryDto> CalculateFareAsync(int flightId, int passengerCount, int extraBaggageKg, bool hasMeal);
    
    // Add-ons
    Task<BookingResponseDto> AddAddOnsAsync(string bookingId, AddOnDto dto);
    
    // Internal (called by Payment Service)
    Task<Booking> GetBookingEntityAsync(string bookingId);
    Task<bool> ReleaseSeatsOnCancellationAsync(string bookingId);
}