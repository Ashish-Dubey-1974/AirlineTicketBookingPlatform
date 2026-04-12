using Microsoft.EntityFrameworkCore;
using SkyBooker.Payment.API.Entities;

namespace SkyBooker.Payment.API.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Payments> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payments>(entity =>
        {
            entity.Property(p => p.Amount).HasPrecision(18, 2);
            entity.Property(p => p.RefundAmount).HasPrecision(18, 2);
            
            entity.HasIndex(p => p.BookingId).IsUnique();
            entity.HasIndex(p => p.TransactionId);
            entity.HasIndex(p => p.Status);
        });
    }
}