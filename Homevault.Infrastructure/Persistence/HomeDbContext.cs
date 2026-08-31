using Homevault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Homevault.Infrastructure.Persistence;

public class HomeDbContext(DbContextOptions<HomeDbContext> options) : DbContext(options)
{
    public DbSet<Home> Homes => Set<Home>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Home>(entity =>
        {
            entity.HasKey(home => home.Id);
            entity.Property(home => home.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(home => home.CreatedAt)
                .IsRequired();
        });
    }
}
