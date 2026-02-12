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

public sealed record DailyWeatherPointDto(
    DateTime DateUtc,
    decimal Temperature,
    decimal FeelsLike,
    int Humidity,
    decimal WindSpeed,
    string Summary,
    string IconCode);

public sealed record WeatherTimelineDto(
    string City,
    string Country,
    TemperatureUnit Units,
    IReadOnlyList<DailyWeatherPointDto> PreviousFiveDays,
    IReadOnlyList<DailyWeatherPointDto> NextFiveDays);
