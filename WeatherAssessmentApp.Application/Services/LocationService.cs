using WeatherAssessmentApp.Application.Abstractions.External;
using WeatherAssessmentApp.Application.Abstractions.Persistence;
using WeatherAssessmentApp.Application.Abstractions.Services;
using WeatherAssessmentApp.Application.Common;
using WeatherAssessmentApp.Application.Dtos;
using WeatherAssessmentApp.Application.Exceptions;
using WeatherAssessmentApp.Application.Models;
using WeatherAssessmentApp.Domain.Entities;

namespace WeatherAssessmentApp.Application.Services;

public sealed class LocationService : ILocationService
{
    private readonly ILocationRepository _locationRepository;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IWeatherSnapshotRepository _snapshotRepository;
    private readonly IWeatherProviderClient _weatherProviderClient;
    private readonly IUnitOfWork _unitOfWork;

    public LocationService(
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

    public async Task<IReadOnlyList<LocationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var locations = await _locationRepository.GetAllAsync(cancellationToken);

        return locations
            .OrderByDescending(x => x.IsFavorite)
            .ThenBy(x => x.City)
            .Select(MapLocation)
            .ToList();
    }

    public async Task<LocationDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var location = await _locationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Location with id '{id}' was not found.");

        return MapLocation(location);
    }

    public async Task<LocationDto> CreateAsync(CreateLocationRequest request, CancellationToken cancellationToken = default)
    {
        var city = NormalizeRequired(request.City, nameof(request.City));
        var country = NormalizeOptional(request.Country);

        var existing = await _locationRepository.GetByCityCountryAsync(city, country, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("Location is already being tracked.");
        }

        var preferences = await EnsurePreferencesAsync(cancellationToken);
        var weather = await _weatherProviderClient.GetCurrentByCityAsync(city, country, preferences.Units, cancellationToken);

        var location = new Location
        {
            City = weather.City,
            Country = weather.Country,
            Latitude = weather.Latitude,
            Longitude = weather.Longitude,
            IsFavorite = request.IsFavorite,
            UserPreferencesId = preferences.Id,
            LastSyncedAtUtc = DateTime.UtcNow,
            LastWeatherFingerprint = WeatherFingerprint.From(weather)
        };

        await _locationRepository.AddAsync(location, cancellationToken);
        await _snapshotRepository.AddAsync(ToSnapshot(location, weather), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapLocation(location, preferences.Units);
    }

    public async Task<LocationDto> UpdateAsync(int id, UpdateLocationRequest request, CancellationToken cancellationToken = default)
    {
        var location = await _locationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Location with id '{id}' was not found.");

        location.IsFavorite = request.IsFavorite ?? location.IsFavorite;

        var requestedCity = request.City is null ? location.City : NormalizeRequired(request.City, nameof(request.City));
        var requestedCountry = request.Country is null ? location.Country : NormalizeOptional(request.Country);
        var cityChanged = !string.Equals(requestedCity, location.City, StringComparison.OrdinalIgnoreCase) ||
                          !string.Equals(requestedCountry, location.Country, StringComparison.OrdinalIgnoreCase);

        if (cityChanged)
        {
            var units = location.UserPreferences?.Units ?? (await EnsurePreferencesAsync(cancellationToken)).Units;
            var weather = await _weatherProviderClient.GetCurrentByCityAsync(requestedCity, requestedCountry, units, cancellationToken);

            location.City = weather.City;
            location.Country = weather.Country;
            location.Latitude = weather.Latitude;
            location.Longitude = weather.Longitude;
            location.LastSyncedAtUtc = DateTime.UtcNow;
            location.LastWeatherFingerprint = WeatherFingerprint.From(weather);

            await _snapshotRepository.AddAsync(ToSnapshot(location, weather), cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapLocation(location);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var location = await _locationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Location with id '{id}' was not found.");

        _locationRepository.Remove(location);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserPreferences> EnsurePreferencesAsync(CancellationToken cancellationToken)
    {
        var preferences = await _preferencesRepository.GetDefaultAsync(cancellationToken);
        if (preferences is not null)
        {
            return preferences;
        }

        var defaultPreferences = new UserPreferences();
        await _preferencesRepository.AddAsync(defaultPreferences, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return defaultPreferences;
    }

    private static WeatherSnapshot ToSnapshot(Location location, ExternalCurrentWeather weather)
    {
        return new WeatherSnapshot
        {
            Location = location,
            ObservedAtUtc = weather.ObservedAtUtc,
            Temperature = weather.Temperature,
            FeelsLike = weather.FeelsLike,
            Humidity = weather.Humidity,
            Pressure = weather.Pressure,
            WindSpeed = weather.WindSpeed,
            Summary = weather.Summary,
            IconCode = weather.IconCode,
            SourcePayload = weather.RawPayload
        };
    }

    private static LocationDto MapLocation(Location location)
    {
        return MapLocation(location, location.UserPreferences?.Units);
    }

    private static LocationDto MapLocation(Location location, WeatherAssessmentApp.Domain.Enums.TemperatureUnit? units)
    {
        return new LocationDto(
            location.Id,
            location.City,
            location.Country,
            location.Latitude,
            location.Longitude,
            location.IsFavorite,
            location.LastSyncedAtUtc,
            units ?? WeatherAssessmentApp.Domain.Enums.TemperatureUnit.Metric);
    }

    private static string NormalizeRequired(string value, string field)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ValidationException($"{field} is required.");
        }

        return normalized;
    }

    private static string NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
