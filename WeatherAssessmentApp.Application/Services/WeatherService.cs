using WeatherAssessmentApp.Application.Abstractions.External;
using WeatherAssessmentApp.Application.Abstractions.Persistence;
using WeatherAssessmentApp.Application.Abstractions.Services;
using WeatherAssessmentApp.Application.Dtos;
using WeatherAssessmentApp.Application.Exceptions;
using WeatherAssessmentApp.Domain.Entities;
using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Application.Services;

public sealed class WeatherService : IWeatherService
{
    private readonly ILocationRepository _locationRepository;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IWeatherSnapshotRepository _snapshotRepository;
    private readonly IWeatherProviderClient _weatherProviderClient;
    private readonly IUnitOfWork _unitOfWork;

    public WeatherService(
        ILocationRepository locationRepository,
        IUserPreferencesRepository preferencesRepository,
        IWeatherSnapshotRepository snapshotRepository,
        IWeatherProviderClient weatherProviderClient,
        IUnitOfWork unitOfWork)
    {
        _locationRepository = locationRepository;
        _preferencesRepository = preferencesRepository;
        _snapshotRepository = snapshotRepository;
        _weatherProviderClient = weatherProviderClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CurrentWeatherDto>> GetCurrentForTrackedLocationsAsync(CancellationToken cancellationToken = default)
    {
        var locations = await _locationRepository.GetAllAsync(cancellationToken);
        var items = new List<CurrentWeatherDto>();

        foreach (var location in locations)
        {
            var latest = location.WeatherSnapshots
                .OrderByDescending(x => x.ObservedAtUtc)
                .FirstOrDefault();

            if (latest is null)
            {
                var units = location.UserPreferences?.Units ?? TemperatureUnit.Metric;
                var liveWeather = await _weatherProviderClient.GetCurrentByCityAsync(location.City, location.Country, units, cancellationToken);
                items.Add(ToCurrentWeatherDto(location.Id, liveWeather, units, location.LastSyncedAtUtc));
                continue;
            }

            items.Add(ToCurrentWeatherDto(location, latest));
        }

        return items
            .OrderByDescending(x => locations.First(l => l.Id == x.LocationId).IsFavorite)
            .ThenBy(x => x.City)
            .ToList();
    }

    public async Task<CurrentWeatherDto> GetCurrentByLocationIdAsync(int locationId, CancellationToken cancellationToken = default)
    {
        var location = await _locationRepository.GetByIdAsync(locationId, cancellationToken)
            ?? throw new NotFoundException($"Location with id '{locationId}' was not found.");

        var units = location.UserPreferences?.Units ?? TemperatureUnit.Metric;
        var weather = await _weatherProviderClient.GetCurrentByCityAsync(location.City, location.Country, units, cancellationToken);
        return ToCurrentWeatherDto(location.Id, weather, units, location.LastSyncedAtUtc);
    }

    public async Task<CurrentWeatherDto> GetCurrentByCityAsync(string city, string? country, TemperatureUnit? units, CancellationToken cancellationToken = default)
    {
        var actualUnits = units ?? (await GetDefaultPreferencesAsync(cancellationToken)).Units;
        var weather = await _weatherProviderClient.GetCurrentByCityAsync(city.Trim(), country?.Trim(), actualUnits, cancellationToken);
        return ToCurrentWeatherDto(null, weather, actualUnits, null);
    }

    public async Task<WeatherForecastDto> GetForecastByLocationIdAsync(int locationId, CancellationToken cancellationToken = default)
    {
        var location = await _locationRepository.GetByIdAsync(locationId, cancellationToken)
            ?? throw new NotFoundException($"Location with id '{locationId}' was not found.");

        var units = location.UserPreferences?.Units ?? TemperatureUnit.Metric;
        var forecast = await _weatherProviderClient.GetFiveDayForecastByCityAsync(location.City, location.Country, units, cancellationToken);

        return new WeatherForecastDto(
            location.City,
            location.Country,
            units,
            forecast.Select(x => new ForecastItemDto(x.ForecastAtUtc, x.Temperature, x.FeelsLike, x.Humidity, x.Summary, x.IconCode, x.WindSpeed)).ToList());
    }

    public async Task<WeatherForecastDto> GetForecastByCityAsync(string city, string? country, TemperatureUnit? units, CancellationToken cancellationToken = default)
    {
        var normalizedCity = city.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCity))
        {
            throw new ValidationException("City is required.");
        }

        var actualUnits = units ?? (await GetDefaultPreferencesAsync(cancellationToken)).Units;
        var forecast = await _weatherProviderClient.GetFiveDayForecastByCityAsync(normalizedCity, country?.Trim(), actualUnits, cancellationToken);

        var current = await _weatherProviderClient.GetCurrentByCityAsync(normalizedCity, country?.Trim(), actualUnits, cancellationToken);

        return new WeatherForecastDto(
            current.City,
            current.Country,
            actualUnits,
            forecast.Select(x => new ForecastItemDto(x.ForecastAtUtc, x.Temperature, x.FeelsLike, x.Humidity, x.Summary, x.IconCode, x.WindSpeed)).ToList());
    }

    private async Task<UserPreferences> GetDefaultPreferencesAsync(CancellationToken cancellationToken)
    {
        var preferences = await _preferencesRepository.GetDefaultAsync(cancellationToken);
        if (preferences is not null)
        {
            return preferences;
        }

        var created = new UserPreferences();
        await _preferencesRepository.AddAsync(created, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    private static CurrentWeatherDto ToCurrentWeatherDto(Location location, WeatherSnapshot snapshot)
    {
        var units = location.UserPreferences?.Units ?? TemperatureUnit.Metric;
        return new CurrentWeatherDto(
            location.Id,
            location.City,
            location.Country,
            snapshot.Temperature,
            snapshot.FeelsLike,
            snapshot.Humidity,
            snapshot.Pressure,
            snapshot.WindSpeed,
            snapshot.Summary,
            snapshot.IconCode,
            snapshot.ObservedAtUtc,
            units,
            location.LastSyncedAtUtc);
    }

    private static CurrentWeatherDto ToCurrentWeatherDto(int? locationId, Models.ExternalCurrentWeather weather, TemperatureUnit units, DateTime? lastSyncedAtUtc)
    {
        return new CurrentWeatherDto(
            locationId,
            weather.City,
            weather.Country,
            weather.Temperature,
            weather.FeelsLike,
            weather.Humidity,
            weather.Pressure,
            weather.WindSpeed,
            weather.Summary,
            weather.IconCode,
            weather.ObservedAtUtc,
            units,
            lastSyncedAtUtc);
    }
}
