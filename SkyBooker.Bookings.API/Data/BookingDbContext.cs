using Microsoft.EntityFrameworkCore;
using SkyBooker.Bookings.API.Entities;

namespace SkyBooker.Bookings.API.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }
    
    public DbSet<Booking> Bookings { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure decimal precision for all money fields
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.TotalFare).HasPrecision(18, 2);
            entity.Property(b => b.BaseFare).HasPrecision(18, 2);
            entity.Property(b => b.Taxes).HasPrecision(18, 2);
            entity.Property(b => b.AncillaryCharges).HasPrecision(18, 2);
            entity.Property(b => b.RefundAmount).HasPrecision(18, 2);
        });
        
        // Set default values
        modelBuilder.Entity<Booking>()
            .Property(b => b.Status)
            .HasDefaultValue(BookingStatusConstants.Pending);
        
        modelBuilder.Entity<Booking>()
            .Property(b => b.BookedAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}