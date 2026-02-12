using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Application.Dtos;

public sealed record ForecastItemDto(
    DateTime ForecastAtUtc,
    decimal Temperature,
    decimal FeelsLike,
    int Humidity,
    string Summary,
    string IconCode,
    decimal WindSpeed);

public sealed record WeatherForecastDto(
    string City,
    string Country,
    TemperatureUnit Units,
    IReadOnlyList<ForecastItemDto> Items);
