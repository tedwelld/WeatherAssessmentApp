using Microsoft.EntityFrameworkCore;
using WeatherAssessmentApp.Application.Abstractions.Persistence;
using WeatherAssessmentApp.Domain.Entities;

namespace WeatherAssessmentApp.Infrastructure.Persistence.Repositories;

public sealed class LocationRepository : ILocationRepository
{
    private readonly WeatherDbContext _dbContext;

    public LocationRepository(WeatherDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Location>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .Include(x => x.UserPreferences)
            .Include(x => x.WeatherSnapshots)
            .ToListAsync(cancellationToken);
    }

    public async Task<Location?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .Include(x => x.UserPreferences)
            .Include(x => x.WeatherSnapshots)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Location?> GetByCityCountryAsync(string city, string country, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .FirstOrDefaultAsync(
                x => x.City.ToLower() == city.ToLower() && x.Country.ToLower() == country.ToLower(),
                cancellationToken);
    }

    public async Task AddAsync(Location location, CancellationToken cancellationToken = default)
    {
        await _dbContext.Locations.AddAsync(location, cancellationToken);
    }

    public void Remove(Location location)
    {
        _dbContext.Locations.Remove(location);
    }
}
