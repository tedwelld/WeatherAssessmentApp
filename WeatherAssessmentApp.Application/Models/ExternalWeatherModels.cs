namespace WeatherAssessmentApp.Application.Models;

public sealed record ExternalCurrentWeather(
    string City,
    string Country,
    double Latitude,
    double Longitude,
    decimal Temperature,
    decimal FeelsLike,
    int Humidity,
    int Pressure,
    decimal WindSpeed,
    string Summary,
    string IconCode,
    DateTime ObservedAtUtc,
    string RawPayload);

public sealed record ExternalForecastItem(
    DateTime ForecastAtUtc,
    decimal Temperature,
    decimal FeelsLike,
    int Humidity,
    decimal WindSpeed,
    string Summary,
    string IconCode);
