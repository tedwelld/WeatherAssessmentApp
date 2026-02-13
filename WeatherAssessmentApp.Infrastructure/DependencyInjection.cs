using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WeatherAssessmentApp.Application.Abstractions.External;
using WeatherAssessmentApp.Application.Abstractions.Persistence;
using WeatherAssessmentApp.Infrastructure.Background;
using WeatherAssessmentApp.Infrastructure.Integrations;
using WeatherAssessmentApp.Infrastructure.Options;
using WeatherAssessmentApp.Infrastructure.Persistence;
using WeatherAssessmentApp.Infrastructure.Persistence.Repositories;

namespace WeatherAssessmentApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenWeatherMapOptions>(configuration.GetSection(OpenWeatherMapOptions.SectionName));
        services.Configure<BackgroundSyncOptions>(configuration.GetSection(BackgroundSyncOptions.SectionName));

        var provider = configuration["Database:Provider"]?.Trim();
        if (!string.IsNullOrWhiteSpace(provider) && !provider.Equals("sqlserver", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only SQL Server is supported. Set Database:Provider=sqlserver.");
        }

        var sqlServerConnection = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("ConnectionStrings:SqlServer is required.");

        services.AddDbContext<WeatherDbContext>(options =>
        {
            options.UseSqlServer(sqlServerConnection);
        });

        services.AddMemoryCache();

        services.AddHttpClient<IWeatherProviderClient, OpenWeatherMapClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OpenWeatherMapOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IWeatherSnapshotRepository, WeatherSnapshotRepository>();
        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
        services.AddScoped<ISyncOperationRepository, SyncOperationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHostedService<WeatherSyncBackgroundService>();

        return services;
    }
}
