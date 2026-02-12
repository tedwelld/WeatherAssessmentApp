using FluentAssertions;
using Moq;
using WeatherAssessmentApp.Application.Abstractions.External;
using WeatherAssessmentApp.Application.Abstractions.Persistence;
using WeatherAssessmentApp.Application.Exceptions;
using WeatherAssessmentApp.Application.Models;
using WeatherAssessmentApp.Application.Services;
using WeatherAssessmentApp.Domain.Entities;
using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Application.Tests.Services;

public class LocationServiceTests
{
    private readonly Mock<ILocationRepository> _locationRepository = new();
    private readonly Mock<IUserPreferencesRepository> _preferencesRepository = new();
    private readonly Mock<IWeatherSnapshotRepository> _snapshotRepository = new();
    private readonly Mock<IWeatherProviderClient> _weatherProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task CreateAsync_ShouldThrowConflict_WhenLocationAlreadyExists()
    {
        var service = CreateService();
        _locationRepository
            .Setup(x => x.GetByCityCountryAsync("London", "GB", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Location { Id = 1, City = "London", Country = "GB", UserPreferencesId = 1 });

        var action = () => service.CreateAsync(new CreateLocationRequest("London", "GB"), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>();
        _weatherProvider.Verify(
            x => x.GetCurrentByCityAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TemperatureUnit>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistLocationAndSnapshot()
    {
        var service = CreateService();

        _locationRepository
            .Setup(x => x.GetByCityCountryAsync("Seattle", "US", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);

        _preferencesRepository
            .Setup(x => x.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPreferences { Id = 7, Units = TemperatureUnit.Metric, RefreshIntervalMinutes = 30 });

        _weatherProvider
            .Setup(x => x.GetCurrentByCityAsync("Seattle", "US", TemperatureUnit.Metric, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalCurrentWeather(
                "Seattle",
                "US",
                47.61,
                -122.33,
                12.4m,
                10.1m,
                80,
                1012,
                2.7m,
                "clear sky",
                "01d",
                DateTime.UtcNow,
                "{}"));

        var result = await service.CreateAsync(new CreateLocationRequest("Seattle", "US", true), CancellationToken.None);

        result.City.Should().Be("Seattle");
        result.Country.Should().Be("US");
        result.IsFavorite.Should().BeTrue();

        _locationRepository.Verify(x => x.AddAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()), Times.Once);
        _snapshotRepository.Verify(x => x.AddAsync(It.IsAny<WeatherSnapshot>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private LocationService CreateService()
    {
        return new LocationService(
            _locationRepository.Object,
            _preferencesRepository.Object,
            _snapshotRepository.Object,
            _weatherProvider.Object,
            _unitOfWork.Object);
    }
}
