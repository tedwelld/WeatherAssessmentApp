using WeatherAssessmentApp.Domain.Entities;

namespace WeatherAssessmentApp.Application.Abstractions.Persistence;

public interface ILocationRepository
{
    Task<IReadOnlyList<Location>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Location?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Location?> GetByCityCountryAsync(string city, string country, CancellationToken cancellationToken = default);
    Task AddAsync(Location location, CancellationToken cancellationToken = default);
    void Remove(Location location);
}
