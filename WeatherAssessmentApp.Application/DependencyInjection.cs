using Microsoft.Extensions.DependencyInjection;
using WeatherAssessmentApp.Application.Abstractions.Services;
using WeatherAssessmentApp.Application.Services;

namespace WeatherAssessmentApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IWeatherService, WeatherService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IWeatherSyncService, WeatherSyncService>();
        services.AddScoped<ISyncHistoryService, SyncHistoryService>();

        return services;
    }
}
