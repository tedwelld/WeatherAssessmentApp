using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Application.Dtos;

public sealed record LocationDto(
    int Id,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    bool IsFavorite,
    DateTime? LastSyncedAtUtc,
    TemperatureUnit Units);
