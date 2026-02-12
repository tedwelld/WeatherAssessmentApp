using WeatherAssessmentApp.Domain.Entities;

namespace WeatherAssessmentApp.Application.Abstractions.Persistence;

public interface IWeatherSnapshotRepository
{
    Task AddAsync(WeatherSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<WeatherSnapshot?> GetLatestByLocationIdAsync(int locationId, CancellationToken cancellationToken = default);
}
