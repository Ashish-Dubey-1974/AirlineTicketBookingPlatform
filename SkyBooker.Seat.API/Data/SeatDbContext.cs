// SkyBooker.Seat.API/Data/SeatDbContext.cs
using Microsoft.EntityFrameworkCore;
using SkyBooker.Seat.API.Entities;

namespace SkyBooker.Seat.API.Data;

public class SeatDbContext : DbContext
{
    public SeatDbContext(DbContextOptions<SeatDbContext> options) : base(options) { }
    
    public DbSet<Seats> Seats { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Seats>(entity =>
        {
            entity.HasIndex(s => new { s.FlightId, s.SeatNumber }).IsUnique();
            entity.Property(s => s.PriceMultiplier).HasPrecision(5, 2);
        });
    }
}