using Microsoft.EntityFrameworkCore;
using WeatherAssessmentApp.Application.Abstractions.Persistence;
using WeatherAssessmentApp.Domain.Entities;

namespace WeatherAssessmentApp.Infrastructure.Persistence.Repositories;

public sealed class WeatherSnapshotRepository : IWeatherSnapshotRepository
{
    private readonly WeatherDbContext _dbContext;

    public WeatherSnapshotRepository(WeatherDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(WeatherSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await _dbContext.WeatherSnapshots.AddAsync(snapshot, cancellationToken);
    }

    public async Task<WeatherSnapshot?> GetLatestByLocationIdAsync(int locationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WeatherSnapshots
            .Where(x => x.LocationId == locationId)
            .OrderByDescending(x => x.ObservedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
