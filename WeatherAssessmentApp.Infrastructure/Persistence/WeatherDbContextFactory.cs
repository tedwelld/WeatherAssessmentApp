using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WeatherAssessmentApp.Infrastructure.Persistence;

public sealed class WeatherDbContextFactory : IDesignTimeDbContextFactory<WeatherDbContext>
{
    public WeatherDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WeatherDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer")
            ?? "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=WeatherAssessmentDb;MultipleActiveResultSets=true;TrustServerCertificate=true;";

        optionsBuilder.UseSqlServer(connectionString);
        return new WeatherDbContext(optionsBuilder.Options);
    }
}
