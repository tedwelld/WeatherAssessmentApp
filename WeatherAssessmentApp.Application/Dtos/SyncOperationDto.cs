using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Application.Dtos;

public sealed record SyncOperationDto(
    int Id,
    SyncOperationType Type,
    int? LocationId,
    string LocationDisplayName,
    int RefreshedLocations,
    int SnapshotsCreated,
    DateTime OccurredAtUtc);
