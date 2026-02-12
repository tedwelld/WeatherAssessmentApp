using Microsoft.AspNetCore.Mvc;
using WeatherAssessmentApp.Application.Abstractions.Services;
using WeatherAssessmentApp.Application.Models;

namespace WeatherAssessmentApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly IWeatherSyncService _weatherSyncService;

    public LocationsController(ILocationService locationService, IWeatherSyncService weatherSyncService)
    {
        _locationService = locationService;
        _weatherSyncService = weatherSyncService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var locations = await _locationService.GetAllAsync(cancellationToken);
        return Ok(locations);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var location = await _locationService.GetByIdAsync(id, cancellationToken);
        return Ok(location);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLocationRequest request, CancellationToken cancellationToken)
    {
        var created = await _locationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        var updated = await _locationService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _locationService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/refresh")]
    public async Task<IActionResult> Refresh(int id, CancellationToken cancellationToken)
    {
        await _weatherSyncService.RefreshLocationAsync(id, cancellationToken);
        return Accepted();
    }
}
