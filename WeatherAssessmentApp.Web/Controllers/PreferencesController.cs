using Microsoft.AspNetCore.Mvc;
using WeatherAssessmentApp.Application.Abstractions.Services;
using WeatherAssessmentApp.Application.Models;

namespace WeatherAssessmentApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PreferencesController : ControllerBase
{
    private readonly IUserPreferencesService _preferencesService;

    public PreferencesController(IUserPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var preferences = await _preferencesService.GetAsync(cancellationToken);
        return Ok(preferences);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateUserPreferencesRequest request, CancellationToken cancellationToken)
    {
        var updated = await _preferencesService.UpdateAsync(request, cancellationToken);
        return Ok(updated);
    }
}
