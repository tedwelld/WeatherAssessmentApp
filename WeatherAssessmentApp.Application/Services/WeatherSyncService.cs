using WeatherAssessmentApp.Application.Abstractions.External;
using WeatherAssessmentApp.Application.Abstractions.Persistence;
using WeatherAssessmentApp.Application.Abstractions.Services;
using WeatherAssessmentApp.Application.Common;
using WeatherAssessmentApp.Application.Exceptions;
using WeatherAssessmentApp.Domain.Entities;
using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Application.Services;

public sealed class WeatherSyncService : IWeatherSyncService
{
    private readonly ILocationRepository _locationRepository;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IWeatherSnapshotRepository _snapshotRepository;
    private readonly ISyncOperationRepository _syncOperationRepository;
    private readonly IWeatherProviderClient _weatherProviderClient;
    private readonly IUnitOfWork _unitOfWork;

    public WeatherSyncService(
        ILocationRepository locationRepository,
        IUserPreferencesRepository preferencesRepository,
        IWeatherSnapshotRepository snapshotRepository,
        ISyncOperationRepository syncOperationRepository,
        IWeatherProviderClient weatherProviderClient,
        IUnitOfWork unitOfWork)
    {
        _locationRepository = locationRepository;
        _preferencesRepository = preferencesRepository;
        _snapshotRepository = snapshotRepository;
        _syncOperationRepository = syncOperationRepository;
        _weatherProviderClient = weatherProviderClient;
        _unitOfWork = unitOfWork;
    }

    public async Task RefreshLocationAsync(int locationId, CancellationToken cancellationToken = default)
    {
        var location = await _locationRepository.GetByIdAsync(locationId, cancellationToken)
            ?? throw new NotFoundException($"Location with id '{locationId}' was not found.");

        var preferences = location.UserPreferences ?? await EnsurePreferencesAsync(cancellationToken);
        var snapshotCreated = await RefreshLocationInternalAsync(location, preferences.Units, cancellationToken);

        await _syncOperationRepository.AddAsync(new SyncOperation
        {
            Type = SyncOperationType.LocationRefresh,
            LocationId = location.Id,
            LocationDisplayName = $"{location.City}, {location.Country}",
            RefreshedLocations = 1,
            SnapshotsCreated = snapshotCreated ? 1 : 0,
            OccurredAtUtc = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RefreshAllAsync(CancellationToken cancellationToken = default)
    {
        var locations = await _locationRepository.GetAllAsync(cancellationToken);
        var snapshotsCreated = 0;
        if (locations.Count > 0)
        {
            var defaultPreferences = await EnsurePreferencesAsync(cancellationToken);

            foreach (var location in locations)
            {
                var units = location.UserPreferences?.Units ?? defaultPreferences.Units;
                var created = await RefreshLocationInternalAsync(location, units, cancellationToken);
                if (created)
                {
                    snapshotsCreated++;
                }
            }
        }

        await _syncOperationRepository.AddAsync(new SyncOperation
        {
            Type = SyncOperationType.RefreshAll,
            LocationId = null,
            LocationDisplayName = "All Tracked Locations",
            RefreshedLocations = locations.Count,
            SnapshotsCreated = snapshotsCreated,
            OccurredAtUtc = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return locations.Count;
    }

    private async Task<bool> RefreshLocationInternalAsync(Location location, TemperatureUnit units, CancellationToken cancellationToken)
    {
        var weather = await _weatherProviderClient.GetCurrentByCityAsync(location.City, location.Country, units, cancellationToken);
        var fingerprint = WeatherFingerprint.From(weather);
        var snapshotCreated = false;

        if (!string.Equals(location.LastWeatherFingerprint, fingerprint, StringComparison.Ordinal))
        {
            await _snapshotRepository.AddAsync(new WeatherSnapshot
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
            }, cancellationToken);
            snapshotCreated = true;
        }

        location.City = weather.City;
        location.Country = weather.Country;
        location.Latitude = weather.Latitude;
        location.Longitude = weather.Longitude;
        location.LastWeatherFingerprint = fingerprint;
        location.LastSyncedAtUtc = DateTime.UtcNow;

        return snapshotCreated;
    }

    private async Task<UserPreferences> EnsurePreferencesAsync(CancellationToken cancellationToken)
    {
        var existing = await _preferencesRepository.GetDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = new UserPreferences();
        await _preferencesRepository.AddAsync(created, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }
}
