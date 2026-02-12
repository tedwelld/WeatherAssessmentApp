namespace WeatherAssessmentApp.Application.Abstractions.Services;

public interface IWeatherSyncService
{
    Task RefreshLocationAsync(int locationId, CancellationToken cancellationToken = default);
    Task<int> RefreshAllAsync(CancellationToken cancellationToken = default);
}
