using Microsoft.EntityFrameworkCore;
using RequestService.Core.Entities;

namespace RequestService.Infrastructure.Data;

public class RequestServiceContext : DbContext
{
    public RequestServiceContext(DbContextOptions<RequestServiceContext> options) : base(options)
    {
    }

    public DbSet<ArtworkRequest> ArtworkRequests { get; set; }
    public DbSet<RequestLog> RequestLogs { get; set; }
    public DbSet<RequestMessage> RequestMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArtworkRequest>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Title).IsRequired();
            entity.Property(r => r.RequesterId).IsRequired();
            entity.Property(r => r.ArtistId).IsRequired();

            // Persist the status as its string name so the DB stays readable.
            entity.Property(r => r.Status).HasConversion<string>();

            // Listing queries filter by the two owner columns.
            entity.HasIndex(r => r.ArtistId);
            entity.HasIndex(r => r.RequesterId);

            entity.HasMany(r => r.Logs)
                .WithOne(l => l.Request)
                .HasForeignKey(l => l.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.Messages)
                .WithOne(m => m.Request)
                .HasForeignKey(m => m.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RequestLog>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Action).IsRequired();
        });

        modelBuilder.Entity<RequestMessage>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Content).IsRequired();
        });
    }
}
