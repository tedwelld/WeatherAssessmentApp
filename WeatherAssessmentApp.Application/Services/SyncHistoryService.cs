using WeatherAssessmentApp.Application.Abstractions.Persistence;
using WeatherAssessmentApp.Application.Abstractions.Services;
using WeatherAssessmentApp.Application.Dtos;

namespace WeatherAssessmentApp.Application.Services;

public sealed class SyncHistoryService : ISyncHistoryService
{
    private readonly ISyncOperationRepository _syncOperationRepository;

    public SyncHistoryService(ISyncOperationRepository syncOperationRepository)
    {
        _syncOperationRepository = syncOperationRepository;
    }

    public async Task<IReadOnlyList<SyncOperationDto>> GetRecentAsync(int take = 20, CancellationToken cancellationToken = default)
    {
        var boundedTake = Math.Clamp(take, 1, 100);
        var operations = await _syncOperationRepository.GetRecentAsync(boundedTake, cancellationToken);

        return operations
            .Select(operation => new SyncOperationDto(
                operation.Id,
                operation.Type,
                operation.LocationId,
                operation.LocationDisplayName,
                operation.RefreshedLocations,
                operation.SnapshotsCreated,
                operation.OccurredAtUtc))
            .ToList();
    }
}
