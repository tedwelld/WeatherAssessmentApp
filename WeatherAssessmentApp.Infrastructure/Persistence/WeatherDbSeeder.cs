using Microsoft.EntityFrameworkCore;
using WeatherAssessmentApp.Domain.Entities;

namespace WeatherAssessmentApp.Infrastructure.Persistence;

public static class WeatherDbSeeder
{
    private static readonly SeedLocation[] SeedLocations =
    [
        new("Bulawayo", "ZW", -20.1489, 28.5331, false),
        new("Gaborone", "BW", -24.6282, 25.9231, false),
        new("Johannesburg", "ZA", -26.2041, 28.0473, true)
    ];

    public static async Task SeedDemoLocationsAsync(this WeatherDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var preferences = await dbContext.UserPreferences
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (preferences is null)
        {
            preferences = new UserPreferences();
            await dbContext.UserPreferences.AddAsync(preferences, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var existing = await dbContext.Locations
            .Select(x => new { x.City, x.Country })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var hasChanges = false;

        foreach (var seed in SeedLocations)
        {
            var alreadyTracked = existing.Any(location =>
                string.Equals(location.City, seed.City, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(location.Country, seed.Country, StringComparison.OrdinalIgnoreCase));

            if (alreadyTracked)
            {
                continue;
            }

            dbContext.Locations.Add(new Location
            {
                City = seed.City,
                Country = seed.Country,
                Latitude = seed.Latitude,
                Longitude = seed.Longitude,
                IsFavorite = seed.IsFavorite,
                UserPreferencesId = preferences.Id,
                LastSyncedAtUtc = now
            });

            hasChanges = true;
        }

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed record SeedLocation(
        string City,
        string Country,
        double Latitude,
        double Longitude,
        bool IsFavorite);
}
