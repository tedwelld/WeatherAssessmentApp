using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeatherAssessmentApp.Application.Abstractions.Services;
using WeatherAssessmentApp.Infrastructure.Options;

namespace WeatherAssessmentApp.Infrastructure.Background;

public sealed class WeatherSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeatherSyncBackgroundService> _logger;
    private readonly IOptions<BackgroundSyncOptions> _syncOptions;

    public WeatherSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<WeatherSyncBackgroundService> logger,
        IOptions<BackgroundSyncOptions> syncOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _syncOptions = syncOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_syncOptions.Value.Enabled)
        {
            _logger.LogInformation("Background sync is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IWeatherSyncService>();
                var preferencesService = scope.ServiceProvider.GetRequiredService<IUserPreferencesService>();

                var preferences = await preferencesService.GetAsync(stoppingToken);
                var refreshedCount = await syncService.RefreshAllAsync(stoppingToken);

                _logger.LogInformation("Background sync refreshed {Count} location(s).", refreshedCount);

                var interval = preferences.RefreshIntervalMinutes <= 0
                    ? _syncOptions.Value.FallbackRefreshIntervalMinutes
                    : preferences.RefreshIntervalMinutes;

                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background sync failed. Retrying in 5 minutes.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
