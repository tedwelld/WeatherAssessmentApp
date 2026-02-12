namespace WeatherAssessmentApp.Infrastructure.Options;

public sealed class BackgroundSyncOptions
{
    public const string SectionName = "BackgroundSync";

    public bool Enabled { get; set; } = true;
    public int FallbackRefreshIntervalMinutes { get; set; } = 30;
}
