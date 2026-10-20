using Homevault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Homevault.Infrastructure.Persistence;

public class HomeDbContext(DbContextOptions<HomeDbContext> options) : DbContext(options)
{
    public DbSet<Home> Homes => Set<Home>();

    public DbSet<WeatherObservation> WeatherObservations => Set<WeatherObservation>();

    public DbSet<ShoppingCategory> ShoppingCategories => Set<ShoppingCategory>();

    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();

    public DbSet<TodoTask> HomeTasks => Set<TodoTask>();

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

        modelBuilder.Entity<ShoppingCategory>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.HasAlternateKey(category => new { category.HomeId, category.Id });
            entity.HasOne(category => category.Home).WithMany().HasForeignKey(category => category.HomeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(category => category.Name).IsRequired().HasMaxLength(40);
            entity.Property(category => category.IsActive).IsRequired();
            entity.HasIndex(category => new { category.HomeId, category.SortOrder, category.Name });
            entity.ToTable(table => table.HasCheckConstraint("CK_ShoppingCategory_Name", "length(trim(Name)) BETWEEN 1 AND 40"));
        });

        modelBuilder.Entity<ShoppingItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasOne(item => item.Home).WithMany().HasForeignKey(item => item.HomeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Category).WithMany()
                .HasForeignKey(item => new { item.HomeId, item.CategoryId })
                .HasPrincipalKey(category => new { category.HomeId, category.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(item => item.Name).IsRequired().HasMaxLength(80);
            entity.Property(item => item.Purchased).IsRequired().HasDefaultValue(false);
            entity.Property(item => item.CreatedAtUtc).HasConversion(dateTimeOffsetToUtcConverter).IsRequired();
            entity.Property(item => item.UpdatedAtUtc).HasConversion(dateTimeOffsetToUtcConverter).IsRequired();
            entity.HasIndex(item => new { item.HomeId, item.CreatedAtUtc, item.Id });
            entity.HasIndex(item => new { item.HomeId, item.Purchased });
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_ShoppingItem_Name", "length(trim(Name)) BETWEEN 1 AND 80");
                table.HasCheckConstraint("CK_ShoppingItem_Quantity", "Quantity BETWEEN 1 AND 999");
            });
        });

        modelBuilder.Entity<TodoTask>(entity =>
        {
            entity.HasKey(task => task.Id);
            entity.HasOne(task => task.Home).WithMany().HasForeignKey(task => task.HomeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(task => task.Title).IsRequired().HasMaxLength(120);
            entity.Property(task => task.Meta).IsRequired().HasMaxLength(60);
            entity.Property(task => task.Assignee).IsRequired().HasMaxLength(40);
            entity.Property(task => task.Done).IsRequired().HasDefaultValue(false);
            entity.Property(task => task.RepeatsDaily).IsRequired().HasDefaultValue(false);
            entity.Property(task => task.CompletedAtUtc).HasConversion(dateTimeOffsetToUtcConverter);
            entity.HasIndex(task => new { task.HomeId, task.SortOrder }).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_HomeTask_Title", "length(trim(Title)) BETWEEN 1 AND 120");
                table.HasCheckConstraint("CK_HomeTask_Meta", "length(trim(Meta)) BETWEEN 1 AND 60");
                table.HasCheckConstraint("CK_HomeTask_Assignee", "length(trim(Assignee)) BETWEEN 1 AND 40");
                table.HasCheckConstraint("CK_HomeTask_SortOrder", "SortOrder > 0");
            });
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
