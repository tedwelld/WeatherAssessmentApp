using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Application.Dtos;

public sealed record CurrentWeatherDto(
    int? LocationId,
    string City,
    string Country,
    decimal Temperature,
    decimal FeelsLike,
    int Humidity,
    int Pressure,
    decimal WindSpeed,
    string Summary,
    string IconCode,
    DateTime ObservedAtUtc,
    TemperatureUnit Units,
    DateTime? LastSyncedAtUtc);
