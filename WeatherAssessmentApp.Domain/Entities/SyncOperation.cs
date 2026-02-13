using WeatherAssessmentApp.Domain.Common;
using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Domain.Entities;

public class SyncOperation : Entity
{
    public SyncOperationType Type { get; set; }
    public int? LocationId { get; set; }
    public Location? Location { get; set; }
    public string LocationDisplayName { get; set; } = string.Empty;
    public int RefreshedLocations { get; set; }
    public int SnapshotsCreated { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
