using Microsoft.EntityFrameworkCore;
using SkyBooker.Passenger.API.Entities;

namespace SkyBooker.Passenger.API.Data;

public class PassengerDbContext : DbContext
{
    public PassengerDbContext(DbContextOptions<PassengerDbContext> options) : base(options) { }

    public DbSet<PassengerInfo> Passengers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PassengerInfo>(entity =>
        {
            entity.HasIndex(p => p.BookingId).HasDatabaseName("IX_passengers_booking_id");
            entity.HasIndex(p => p.PassportNumber).HasDatabaseName("IX_passengers_passport_number");
            entity.HasIndex(p => p.TicketNumber).HasDatabaseName("IX_passengers_ticket_number");
            entity.HasIndex(p => p.SeatId).HasDatabaseName("IX_passengers_seat_id");
        });
    }
}