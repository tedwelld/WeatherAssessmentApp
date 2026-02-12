using WeatherAssessmentApp.Domain.Common;
using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Domain.Entities;

public class UserPreferences : Entity
{
    public TemperatureUnit Units { get; set; } = TemperatureUnit.Metric;
    public int RefreshIntervalMinutes { get; set; } = 30;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Location> Locations { get; set; } = new List<Location>();
}
