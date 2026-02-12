using Microsoft.AspNetCore.Mvc;
using WeatherAssessmentApp.Application.Abstractions.Services;

namespace WeatherAssessmentApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SyncController : ControllerBase
{
    private readonly IWeatherSyncService _weatherSyncService;

    public SyncController(IWeatherSyncService weatherSyncService)
    {
        _weatherSyncService = weatherSyncService;
    }

    [HttpPost("refresh-all")]
    public async Task<IActionResult> RefreshAll(CancellationToken cancellationToken)
    {
        var count = await _weatherSyncService.RefreshAllAsync(cancellationToken);
        return Ok(new { refreshedLocations = count, refreshedAtUtc = DateTime.UtcNow });
    }
}
