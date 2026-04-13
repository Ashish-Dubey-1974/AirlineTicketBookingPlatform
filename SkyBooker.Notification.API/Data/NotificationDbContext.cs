using Microsoft.EntityFrameworkCore;
using SkyBooker.Notification.API.Entities;

namespace SkyBooker.Notification.API.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Notifications> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notifications>(entity =>
        {
            entity.HasIndex(n => n.RecipientId);
            entity.HasIndex(n => n.IsRead);
            entity.HasIndex(n => n.SentAt);
            entity.HasIndex(n => n.RelatedBookingId);
            entity.HasIndex(n => n.Type);
        });
    }
}