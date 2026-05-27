using artspace.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace artspace.Infrastructure.Data;

public class ArtSpaceContext : DbContext
{
    public ArtSpaceContext(DbContextOptions<ArtSpaceContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Username).IsRequired();
            entity.Property(u => u.Role).IsRequired();
        });
    }
}