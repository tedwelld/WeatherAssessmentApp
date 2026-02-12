using Microsoft.EntityFrameworkCore;
using WeatherAssessmentApp.Application.Abstractions.Persistence;
using WeatherAssessmentApp.Application.Exceptions;

namespace WeatherAssessmentApp.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly WeatherDbContext _dbContext;

    public UnitOfWork(WeatherDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new ConflictException("A record with the same unique fields already exists.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("Data was changed by another process. Retry the operation.", ex);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;
    }
}
