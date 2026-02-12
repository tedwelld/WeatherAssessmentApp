using WeatherAssessmentApp.Application.Dtos;
using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Application.Abstractions.Services;

public interface IWeatherService
{
    Task<IReadOnlyList<CurrentWeatherDto>> GetCurrentForTrackedLocationsAsync(CancellationToken cancellationToken = default);
    Task<CurrentWeatherDto> GetCurrentByLocationIdAsync(int locationId, CancellationToken cancellationToken = default);
    Task<CurrentWeatherDto> GetCurrentByCityAsync(string city, string? country, TemperatureUnit? units, CancellationToken cancellationToken = default);
    Task<WeatherForecastDto> GetForecastByLocationIdAsync(int locationId, CancellationToken cancellationToken = default);
    Task<WeatherForecastDto> GetForecastByCityAsync(string city, string? country, TemperatureUnit? units, CancellationToken cancellationToken = default);
}
