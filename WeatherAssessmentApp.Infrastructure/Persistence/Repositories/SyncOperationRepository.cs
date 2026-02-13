using Microsoft.EntityFrameworkCore;
using WeatherAssessmentApp.Application.Abstractions.Persistence;
using WeatherAssessmentApp.Domain.Entities;

namespace WeatherAssessmentApp.Infrastructure.Persistence.Repositories;

public sealed class SyncOperationRepository : ISyncOperationRepository
{
    private readonly WeatherDbContext _dbContext;

    public SyncOperationRepository(WeatherDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SyncOperation>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SyncOperations
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SyncOperation operation, CancellationToken cancellationToken = default)
    {
        await _dbContext.SyncOperations.AddAsync(operation, cancellationToken);
    }
}
