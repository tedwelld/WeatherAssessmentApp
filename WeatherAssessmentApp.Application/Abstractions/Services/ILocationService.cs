using WeatherAssessmentApp.Application.Dtos;
using WeatherAssessmentApp.Application.Models;

namespace WeatherAssessmentApp.Application.Abstractions.Services;

public interface ILocationService
{
    Task<IReadOnlyList<LocationDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<LocationDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<LocationDto> CreateAsync(CreateLocationRequest request, CancellationToken cancellationToken = default);
    Task<LocationDto> UpdateAsync(int id, UpdateLocationRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
