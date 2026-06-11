using CommentService.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommentService.Infrastructure.Data;

public class CommentServiceContext : DbContext
{
    public CommentServiceContext(DbContextOptions<CommentServiceContext> options) : base(options)
    {
    }

    public DbSet<Comment> Comments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Content).IsRequired();
            entity.Property(c => c.ArtworkId).IsRequired();
            entity.Property(c => c.UserId).IsRequired();

            // Self-reference: a parent review owns many replies; deleting the
            // parent cascades to its replies.
            entity.HasMany(c => c.Replies)
                .WithOne(c => c.Parent)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Backs the overall-rating aggregate (parent comments per artwork).
            entity.HasIndex(c => new { c.ArtworkId, c.Rating });
        });
    }
}