using Microsoft.EntityFrameworkCore;
using SkyBooker.Flights.API.Entities;

namespace SkyBooker.Flights.API.Data;

public class FlightDbContext : DbContext
{
    public FlightDbContext(DbContextOptions<FlightDbContext> options) 
        : base(options) { }
    
    public DbSet<Flight> Flights { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure decimal precision
        modelBuilder.Entity<Flight>()
            .Property(f => f.BasePrice)
            .HasPrecision(18, 2);
    }
}