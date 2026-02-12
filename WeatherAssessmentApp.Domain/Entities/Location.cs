using WeatherAssessmentApp.Domain.Common;

namespace WeatherAssessmentApp.Domain.Entities;

public class Location : Entity
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime? LastSyncedAtUtc { get; set; }
    public string? LastWeatherFingerprint { get; set; }

    public int UserPreferencesId { get; set; }
    public UserPreferences UserPreferences { get; set; } = null!;

    public byte[] RowVersion { get; set; } = [];

    public ICollection<WeatherSnapshot> WeatherSnapshots { get; set; } = new List<WeatherSnapshot>();
}
