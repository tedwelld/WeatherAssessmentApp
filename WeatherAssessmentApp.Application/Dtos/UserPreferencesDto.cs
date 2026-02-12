using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Application.Dtos;

public sealed record UserPreferencesDto(
    int Id,
    TemperatureUnit Units,
    int RefreshIntervalMinutes,
    DateTime UpdatedAtUtc);
