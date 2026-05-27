using ArtService.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArtService.Infrastructure.Data;

public class ArtServiceContext : DbContext
{
    public ArtServiceContext(DbContextOptions<ArtServiceContext> options) : base(options)
    {
    }

    public DbSet<Artwork> Artworks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Artwork>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Title).IsRequired();
            entity.Property(a => a.ImageUrl).IsRequired();
            entity.Property(a => a.ArtistId).IsRequired();
        });
    }
}