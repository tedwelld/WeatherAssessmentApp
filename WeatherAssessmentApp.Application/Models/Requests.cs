namespace WeatherAssessmentApp.Application.Models;

public sealed record CreateLocationRequest(
    string City,
    string? Country,
    bool IsFavorite = false);

public sealed record UpdateLocationRequest(
    string? City,
    string? Country,
    bool? IsFavorite);

public sealed record UpdateUserPreferencesRequest(
    string Units,
    int RefreshIntervalMinutes);
