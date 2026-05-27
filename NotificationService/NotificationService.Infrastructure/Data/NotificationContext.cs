using NotificationService.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Infrastructure.Data;

public class NotificationContext : DbContext
{
    public NotificationContext(DbContextOptions<NotificationContext> options) : base(options)
    {
    }

    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Message).IsRequired();
            entity.Property(n => n.UserId).IsRequired();
        });
    }
}