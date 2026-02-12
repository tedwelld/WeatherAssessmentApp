using WeatherAssessmentApp.Application.Dtos;
using WeatherAssessmentApp.Application.Models;

namespace WeatherAssessmentApp.Application.Abstractions.Services;

public interface IUserPreferencesService
{
    Task<UserPreferencesDto> GetAsync(CancellationToken cancellationToken = default);
    Task<UserPreferencesDto> UpdateAsync(UpdateUserPreferencesRequest request, CancellationToken cancellationToken = default);
}
