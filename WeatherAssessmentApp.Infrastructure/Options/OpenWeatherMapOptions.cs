namespace WeatherAssessmentApp.Infrastructure.Options;

public sealed class OpenWeatherMapOptions
{
    public const string SectionName = "OpenWeatherMap";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openweathermap.org";
    public int CacheDurationMinutes { get; set; } = 5;
}
