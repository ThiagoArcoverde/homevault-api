using Homevault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Homevault.Infrastructure.Persistence;

public class HomeDbContext(DbContextOptions<HomeDbContext> options) : DbContext(options)
{
    public DbSet<Home> Homes => Set<Home>();

    public DbSet<WeatherObservation> WeatherObservations => Set<WeatherObservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateTimeOffsetToUtcConverter = new ValueConverter<DateTimeOffset, DateTime>(
            value => value.UtcDateTime,
            value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        modelBuilder.Entity<Home>(entity =>
        {
            entity.HasKey(home => home.Id);
            entity.Property(home => home.Name)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(home => home.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<WeatherObservation>(entity =>
        {
            entity.HasKey(observation => observation.Id);
            entity.Property(observation => observation.City)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(observation => observation.State)
                .IsRequired()
                .HasMaxLength(2);
            entity.Property(observation => observation.Latitude)
                .HasPrecision(9, 6)
                .IsRequired();
            entity.Property(observation => observation.Longitude)
                .HasPrecision(9, 6)
                .IsRequired();
            entity.Property(observation => observation.TemperatureCelsius)
                .HasPrecision(5, 2)
                .IsRequired();
            entity.Property(observation => observation.RelativeHumidity)
                .HasPrecision(5, 2)
                .IsRequired();
            entity.Property(observation => observation.Condition)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(observation => observation.IsDay)
                .IsRequired();
            entity.Property(observation => observation.ObservedAt)
                .HasConversion(dateTimeOffsetToUtcConverter)
                .IsRequired();
            entity.Property(observation => observation.CollectedAt)
                .HasConversion(dateTimeOffsetToUtcConverter)
                .IsRequired();
            entity.HasIndex(observation => new
            {
                observation.City,
                observation.State,
                observation.ObservedAt
            });
        });
    }
}
