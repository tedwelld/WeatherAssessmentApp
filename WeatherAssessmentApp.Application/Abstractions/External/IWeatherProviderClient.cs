using WeatherAssessmentApp.Application.Models;
using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Application.Abstractions.External;

public interface IWeatherProviderClient
{
    Task<ExternalCurrentWeather> GetCurrentByCityAsync(
        string city,
        string? country,
        TemperatureUnit units,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalForecastItem>> GetFiveDayForecastByCityAsync(
        string city,
        string? country,
        TemperatureUnit units,
        CancellationToken cancellationToken = default);
}
