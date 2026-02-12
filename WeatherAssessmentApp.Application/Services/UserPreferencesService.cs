using WeatherAssessmentApp.Application.Abstractions.Persistence;
using WeatherAssessmentApp.Application.Abstractions.Services;
using WeatherAssessmentApp.Application.Dtos;
using WeatherAssessmentApp.Application.Exceptions;
using WeatherAssessmentApp.Application.Models;
using WeatherAssessmentApp.Domain.Entities;
using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Application.Services;

public sealed class UserPreferencesService : IUserPreferencesService
{
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserPreferencesService(IUserPreferencesRepository preferencesRepository, IUnitOfWork unitOfWork)
    {
        _preferencesRepository = preferencesRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserPreferencesDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var preferences = await EnsurePreferencesAsync(cancellationToken);
        return Map(preferences);
    }

    public async Task<UserPreferencesDto> UpdateAsync(UpdateUserPreferencesRequest request, CancellationToken cancellationToken = default)
    {
        if (request.RefreshIntervalMinutes is < 5 or > 1440)
        {
            throw new ValidationException("RefreshIntervalMinutes must be between 5 and 1440.");
        }

        var units = ParseUnits(request.Units);
        var preferences = await EnsurePreferencesAsync(cancellationToken);

        preferences.Units = units;
        preferences.RefreshIntervalMinutes = request.RefreshIntervalMinutes;
        preferences.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(preferences);
    }

    private async Task<UserPreferences> EnsurePreferencesAsync(CancellationToken cancellationToken)
    {
        var preferences = await _preferencesRepository.GetDefaultAsync(cancellationToken);
        if (preferences is not null)
        {
            return preferences;
        }

        var created = new UserPreferences();
        await _preferencesRepository.AddAsync(created, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    private static TemperatureUnit ParseUnits(string units)
    {
        if (string.Equals(units, "metric", StringComparison.OrdinalIgnoreCase))
        {
            return TemperatureUnit.Metric;
        }

        if (string.Equals(units, "imperial", StringComparison.OrdinalIgnoreCase))
        {
            return TemperatureUnit.Imperial;
        }

        throw new ValidationException("Units must be 'metric' or 'imperial'.");
    }

    private static UserPreferencesDto Map(UserPreferences preferences)
    {
        return new UserPreferencesDto(
            preferences.Id,
            preferences.Units,
            preferences.RefreshIntervalMinutes,
            preferences.UpdatedAtUtc);
    }
}
