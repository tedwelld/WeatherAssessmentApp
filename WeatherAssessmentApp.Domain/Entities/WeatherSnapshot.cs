using WeatherAssessmentApp.Domain.Common;

namespace WeatherAssessmentApp.Domain.Entities;

public class WeatherSnapshot : Entity
{
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public DateTime ObservedAtUtc { get; set; }
    public decimal Temperature { get; set; }
    public decimal FeelsLike { get; set; }
    public int Humidity { get; set; }
    public int Pressure { get; set; }
    public decimal WindSpeed { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string IconCode { get; set; } = string.Empty;
    public string SourcePayload { get; set; } = string.Empty;
}
