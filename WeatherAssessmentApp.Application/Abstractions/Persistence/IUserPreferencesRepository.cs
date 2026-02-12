using WeatherAssessmentApp.Domain.Entities;

namespace WeatherAssessmentApp.Application.Abstractions.Persistence;

public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetDefaultAsync(CancellationToken cancellationToken = default);
    Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
}
