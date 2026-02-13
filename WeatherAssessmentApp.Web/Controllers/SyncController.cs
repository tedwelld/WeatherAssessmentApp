using Microsoft.AspNetCore.Mvc;
using WeatherAssessmentApp.Application.Abstractions.Services;

namespace WeatherAssessmentApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SyncController : ControllerBase
{
    private readonly IWeatherSyncService _weatherSyncService;
    private readonly ISyncHistoryService _syncHistoryService;

    public SyncController(IWeatherSyncService weatherSyncService, ISyncHistoryService syncHistoryService)
    {
        _weatherSyncService = weatherSyncService;
        _syncHistoryService = syncHistoryService;
    }

    [HttpPost("refresh-all")]
    public async Task<IActionResult> RefreshAll(CancellationToken cancellationToken)
    {
        var count = await _weatherSyncService.RefreshAllAsync(cancellationToken);
        return Ok(new { refreshedLocations = count, refreshedAtUtc = DateTime.UtcNow });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var history = await _syncHistoryService.GetRecentAsync(take, cancellationToken);
        return Ok(history);
    }
}
