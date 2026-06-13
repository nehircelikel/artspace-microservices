using CommissionService.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommissionService.Infrastructure.Data;

public class CommissionServiceContext : DbContext
{
    public CommissionServiceContext(DbContextOptions<CommissionServiceContext> options) : base(options)
    {
    }

    public DbSet<Commission> Commissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Commission>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Title).IsRequired();
            entity.Property(c => c.RequesterId).IsRequired();
            entity.Property(c => c.ArtistId).IsRequired();
            entity.Property(c => c.Status).IsRequired();
        });
    }
}
