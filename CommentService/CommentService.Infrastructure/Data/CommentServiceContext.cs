using CommentService.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommentService.Infrastructure.Data;

public class CommentServiceContext : DbContext
{
    public CommentServiceContext(DbContextOptions<CommentServiceContext> options) : base(options)
    {
    }

    public DbSet<Comment> Comments { get; set; }
    public DbSet<Like> Likes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Content).IsRequired();
            entity.Property(c => c.ArtworkId).IsRequired();
            entity.Property(c => c.UserId).IsRequired();
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.ArtworkId).IsRequired();
            entity.Property(l => l.UserId).IsRequired();
            entity.HasIndex(l => new { l.ArtworkId, l.UserId }).IsUnique();
        });
    }
}