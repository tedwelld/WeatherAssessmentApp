using Microsoft.EntityFrameworkCore;
using WeatherAssessmentApp.Domain.Entities;
using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Infrastructure.Persistence;

public sealed class WeatherDbContext : DbContext
{
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options)
    {
    }

    public DbSet<Location> Locations => Set<Location>();
    public DbSet<WeatherSnapshot> WeatherSnapshots => Set<WeatherSnapshot>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<SyncOperation> SyncOperations => Set<SyncOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.ToTable("UserPreferences");
            entity.Property(x => x.Units)
                .HasConversion(
                    value => value.ToString(),
                    value => Enum.Parse<TemperatureUnit>(value, true));
            entity.Property(x => x.RefreshIntervalMinutes).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("Locations");
            entity.Property(x => x.City).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Country).HasMaxLength(64).IsRequired();
            entity.Property(x => x.LastWeatherFingerprint).HasMaxLength(128);
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasIndex(x => new { x.City, x.Country }).IsUnique();

            entity.HasOne(x => x.UserPreferences)
                .WithMany(x => x.Locations)
                .HasForeignKey(x => x.UserPreferencesId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WeatherSnapshot>(entity =>
        {
            entity.ToTable("WeatherSnapshots");
            entity.Property(x => x.ObservedAtUtc).IsRequired();
            entity.Property(x => x.Temperature).HasColumnType("decimal(8,2)");
            entity.Property(x => x.FeelsLike).HasColumnType("decimal(8,2)");
            entity.Property(x => x.WindSpeed).HasColumnType("decimal(8,2)");
            entity.Property(x => x.Summary).HasMaxLength(256).IsRequired();
            entity.Property(x => x.IconCode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.SourcePayload).IsRequired();

            entity.HasOne(x => x.Location)
                .WithMany(x => x.WeatherSnapshots)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.LocationId, x.ObservedAtUtc });
        });

        modelBuilder.Entity<SyncOperation>(entity =>
        {
            entity.ToTable("SyncOperations");
            entity.Property(x => x.Type)
                .HasConversion(
                    value => value.ToString(),
                    value => Enum.Parse<SyncOperationType>(value, true));
            entity.Property(x => x.LocationDisplayName).HasMaxLength(192).IsRequired();
            entity.Property(x => x.RefreshedLocations).IsRequired();
            entity.Property(x => x.SnapshotsCreated).IsRequired();
            entity.Property(x => x.OccurredAtUtc).IsRequired();

            entity.HasOne(x => x.Location)
                .WithMany()
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.OccurredAtUtc);
        });
    }
}
