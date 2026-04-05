using Microsoft.EntityFrameworkCore;
using SkyBooker.Auth.Entities;

namespace SkyBooker.Auth.Data;

/// <summary>
/// EF Core DbContext for the Auth/User microservice.
/// Each microservice owns its own DbContext and database schema.
/// </summary>
public class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User entity configuration ─────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            // All indexes defined here only — NOT duplicated on the entity class via [Index]
            entity.HasIndex(u => u.Email)
                  .IsUnique()
                  .HasDatabaseName("IX_users_email");

            entity.HasIndex(u => u.Phone)
                  .HasDatabaseName("IX_users_phone");

            entity.HasIndex(u => u.PassportNumber)
                  .HasDatabaseName("IX_users_passport_number");

            entity.Property(u => u.Role)
                  .HasDefaultValue(UserRoles.Passenger);

            entity.Property(u => u.Provider)
                  .HasDefaultValue(AuthProviders.Local);

            entity.Property(u => u.IsActive)
                  .HasDefaultValue(true);

            entity.Property(u => u.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(u => u.UpdatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");
        });
    }
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<User>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
