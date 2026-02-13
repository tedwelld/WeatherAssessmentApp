using WeatherAssessmentApp.Application.Dtos;

namespace WeatherAssessmentApp.Application.Abstractions.Services;

public interface ISyncHistoryService
{
    Task<IReadOnlyList<SyncOperationDto>> GetRecentAsync(int take = 20, CancellationToken cancellationToken = default);
}
