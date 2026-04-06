using Microsoft.EntityFrameworkCore;
using SkyBooker.Airline.Entities;

namespace SkyBooker.Airline.Data;

/// <summary>
/// EF Core DbContext for the Airline and Airport microservice.
/// Contains: Airlines, Airports, and AirlineAirports (many-to-many join).
/// All indexes and constraints are defined here in OnModelCreating — not duplicated on entities.
/// </summary>
public class AirlineDbContext : DbContext
{
    public AirlineDbContext(DbContextOptions<AirlineDbContext> options) : base(options) { }

    public DbSet<Entities.Airline> Airlines { get; set; }
    public DbSet<Airport> Airports { get; set; }
    public DbSet<AirlineAirport> AirlineAirports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Airline configuration ─────────────────────────────────────────────
        modelBuilder.Entity<Entities.Airline>(entity =>
        {
            // IataCode must be unique — used by flight search autocomplete
            entity.HasIndex(a => a.IataCode)
                  .IsUnique()
                  .HasDatabaseName("IX_airlines_iata_code");

            entity.Property(a => a.IsActive)
                  .HasDefaultValue(true);

            entity.Property(a => a.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(a => a.UpdatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");
        });

        // ── Airport configuration ─────────────────────────────────────────────
        modelBuilder.Entity<Airport>(entity =>
        {
            // IataCode must be unique (DEL, BOM, BLR, etc.)
            entity.HasIndex(a => a.IataCode)
                  .IsUnique()
                  .HasDatabaseName("IX_airports_iata_code");

            entity.HasIndex(a => a.City)
                  .HasDatabaseName("IX_airports_city");

            entity.HasIndex(a => a.Country)
                  .HasDatabaseName("IX_airports_country");

            entity.Property(a => a.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(a => a.UpdatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");
        });

        // ── AirlineAirport (join table) — composite PK ────────────────────────
        modelBuilder.Entity<AirlineAirport>(entity =>
        {
            // Composite primary key
            entity.HasKey(aa => new { aa.AirlineId, aa.AirportId });

            // FK: AirlineAirport → Airline
            entity.HasOne(aa => aa.Airline)
                  .WithMany(a => a.AirlineAirports)
                  .HasForeignKey(aa => aa.AirlineId)
                  .OnDelete(DeleteBehavior.Cascade);

            // FK: AirlineAirport → Airport
            entity.HasOne(aa => aa.Airport)
                  .WithMany(ap => ap.AirlineAirports)
                  .HasForeignKey(aa => aa.AirportId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(aa => aa.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");
        });
    }

    /// <summary>Auto-update UpdatedAt on every SaveChanges for Airline and Airport.</summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Entities.Airline>()
                     .Where(e => e.State == EntityState.Modified))
            entry.Entity.UpdatedAt = now;

        foreach (var entry in ChangeTracker.Entries<Airport>()
                     .Where(e => e.State == EntityState.Modified))
            entry.Entity.UpdatedAt = now;

        return base.SaveChangesAsync(cancellationToken);
    }
}
