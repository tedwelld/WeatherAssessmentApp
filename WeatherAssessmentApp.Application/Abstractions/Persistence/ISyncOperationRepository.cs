using WeatherAssessmentApp.Domain.Entities;

namespace WeatherAssessmentApp.Application.Abstractions.Persistence;

public interface ISyncOperationRepository
{
    Task<IReadOnlyList<SyncOperation>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
    Task AddAsync(SyncOperation operation, CancellationToken cancellationToken = default);
}
