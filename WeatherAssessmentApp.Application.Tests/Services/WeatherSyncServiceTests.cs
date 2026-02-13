using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Moq;
using WeatherAssessmentApp.Application.Abstractions.External;
using WeatherAssessmentApp.Application.Abstractions.Persistence;
using WeatherAssessmentApp.Application.Models;
using WeatherAssessmentApp.Application.Services;
using WeatherAssessmentApp.Domain.Entities;
using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Application.Tests.Services;

public class WeatherSyncServiceTests
{
    private readonly Mock<ILocationRepository> _locationRepository = new();
    private readonly Mock<IUserPreferencesRepository> _preferencesRepository = new();
    private readonly Mock<IWeatherSnapshotRepository> _snapshotRepository = new();
    private readonly Mock<ISyncOperationRepository> _syncOperationRepository = new();
    private readonly Mock<IWeatherProviderClient> _weatherProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task RefreshLocationAsync_ShouldAddSnapshot_WhenWeatherChanges()
    {
        var service = CreateService();

        var location = new Location
        {
            Id = 5,
            City = "Madrid",
            Country = "ES",
            UserPreferences = new UserPreferences { Id = 1, Units = TemperatureUnit.Metric },
            LastWeatherFingerprint = "old"
        };

        var external = new ExternalCurrentWeather(
            "Madrid",
            "ES",
            40.4,
            -3.7,
            18.5m,
            18.1m,
            42,
            1010,
            3.1m,
            "few clouds",
            "02d",
            DateTime.UtcNow,
            "{\"ok\":true}");

        _locationRepository.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(location);
        _weatherProvider
            .Setup(x => x.GetCurrentByCityAsync("Madrid", "ES", TemperatureUnit.Metric, It.IsAny<CancellationToken>()))
            .ReturnsAsync(external);

        await service.RefreshLocationAsync(5, CancellationToken.None);

        _snapshotRepository.Verify(x => x.AddAsync(It.IsAny<WeatherSnapshot>(), It.IsAny<CancellationToken>()), Times.Once);
        _syncOperationRepository.Verify(x => x.AddAsync(It.IsAny<SyncOperation>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        location.LastWeatherFingerprint.Should().Be(ComputeFingerprint(external));
        location.LastSyncedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshLocationAsync_ShouldNotAddSnapshot_WhenWeatherUnchanged()
    {
        var service = CreateService();

        var external = new ExternalCurrentWeather(
            "Dublin",
            "IE",
            53.34,
            -6.26,
            9.2m,
            8.8m,
            91,
            1004,
            5.0m,
            "light rain",
            "10d",
            DateTime.UtcNow,
            "{}");

        var location = new Location
        {
            Id = 8,
            City = "Dublin",
            Country = "IE",
            UserPreferences = new UserPreferences { Id = 2, Units = TemperatureUnit.Metric },
            LastWeatherFingerprint = ComputeFingerprint(external)
        };

        _locationRepository.Setup(x => x.GetByIdAsync(8, It.IsAny<CancellationToken>())).ReturnsAsync(location);
        _weatherProvider
            .Setup(x => x.GetCurrentByCityAsync("Dublin", "IE", TemperatureUnit.Metric, It.IsAny<CancellationToken>()))
            .ReturnsAsync(external);

        await service.RefreshLocationAsync(8, CancellationToken.None);

        _snapshotRepository.Verify(x => x.AddAsync(It.IsAny<WeatherSnapshot>(), It.IsAny<CancellationToken>()), Times.Never);
        _syncOperationRepository.Verify(x => x.AddAsync(It.IsAny<SyncOperation>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private WeatherSyncService CreateService()
    {
        return new WeatherSyncService(
            _locationRepository.Object,
            _preferencesRepository.Object,
            _snapshotRepository.Object,
            _syncOperationRepository.Object,
            _weatherProvider.Object,
            _unitOfWork.Object);
    }

    private static string ComputeFingerprint(ExternalCurrentWeather weather)
    {
        var raw = string.Join(
            '|',
            weather.Temperature,
            weather.FeelsLike,
            weather.Humidity,
            weather.Pressure,
            weather.WindSpeed,
            weather.Summary,
            weather.IconCode,
            weather.ObservedAtUtc.ToUniversalTime().ToString("O"));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }
}
