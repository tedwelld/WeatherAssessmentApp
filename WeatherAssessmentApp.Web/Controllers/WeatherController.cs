using Microsoft.AspNetCore.Mvc;
using WeatherAssessmentApp.Application.Abstractions.Services;
using WeatherAssessmentApp.Domain.Enums;

namespace WeatherAssessmentApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetTrackedCurrent(CancellationToken cancellationToken)
    {
        var weather = await _weatherService.GetCurrentForTrackedLocationsAsync(cancellationToken);
        return Ok(weather);
    }

    [HttpGet("current/{locationId:int}")]
    public async Task<IActionResult> GetCurrentByLocation(int locationId, CancellationToken cancellationToken)
    {
        var weather = await _weatherService.GetCurrentByLocationIdAsync(locationId, cancellationToken);
        return Ok(weather);
    }

    [HttpGet("forecast/{locationId:int}")]
    public async Task<IActionResult> GetForecastByLocation(int locationId, CancellationToken cancellationToken)
    {
        var forecast = await _weatherService.GetForecastByLocationIdAsync(locationId, cancellationToken);
        return Ok(forecast);
    }

    [HttpGet("by-city/current")]
    public async Task<IActionResult> GetCurrentByCity(
        [FromQuery] string city,
        [FromQuery] string? country,
        [FromQuery] string? units,
        CancellationToken cancellationToken)
    {
        var parsedUnits = ParseUnits(units);
        var weather = await _weatherService.GetCurrentByCityAsync(city, country, parsedUnits, cancellationToken);
        return Ok(weather);
    }

    [HttpGet("by-city/forecast")]
    public async Task<IActionResult> GetForecastByCity(
        [FromQuery] string city,
        [FromQuery] string? country,
        [FromQuery] string? units,
        CancellationToken cancellationToken)
    {
        var parsedUnits = ParseUnits(units);
        var forecast = await _weatherService.GetForecastByCityAsync(city, country, parsedUnits, cancellationToken);
        return Ok(forecast);
    }

    private static TemperatureUnit? ParseUnits(string? units)
    {
        if (string.IsNullOrWhiteSpace(units))
        {
            return null;
        }

        return units.Trim().ToLowerInvariant() switch
        {
            "metric" => TemperatureUnit.Metric,
            "imperial" => TemperatureUnit.Imperial,
            _ => throw new WeatherAssessmentApp.Application.Exceptions.ValidationException("Units must be metric or imperial.")
        };
    }
}
