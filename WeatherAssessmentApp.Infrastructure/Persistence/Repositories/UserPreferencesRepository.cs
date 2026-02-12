using Microsoft.EntityFrameworkCore;
using WeatherAssessmentApp.Application.Abstractions.Persistence;
using WeatherAssessmentApp.Domain.Entities;

namespace WeatherAssessmentApp.Infrastructure.Persistence.Repositories;

public sealed class UserPreferencesRepository : IUserPreferencesRepository
{
    private readonly WeatherDbContext _dbContext;

    public UserPreferencesRepository(WeatherDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserPreferences?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserPreferences
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserPreferences.AddAsync(preferences, cancellationToken);
    }
}
