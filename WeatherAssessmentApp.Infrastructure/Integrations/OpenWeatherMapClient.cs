using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WeatherAssessmentApp.Application.Abstractions.External;
using WeatherAssessmentApp.Application.Exceptions;
using WeatherAssessmentApp.Application.Models;
using WeatherAssessmentApp.Domain.Enums;
using WeatherAssessmentApp.Infrastructure.Options;

namespace WeatherAssessmentApp.Infrastructure.Integrations;

public sealed class OpenWeatherMapClient : IWeatherProviderClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly IReadOnlyDictionary<string, SeededCityProfile> SeededProfiles =
        new Dictionary<string, SeededCityProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["bulawayo"] = new(
                "Bulawayo",
                "ZW",
                -20.1489,
                28.5331,
                [
                    new DailySeed(27.8m, 28.7m, 43, 4.4m, "clear sky", "01d"),
                    new DailySeed(28.2m, 29.1m, 41, 4.6m, "few clouds", "02d"),
                    new DailySeed(26.9m, 27.8m, 48, 4.2m, "scattered clouds", "03d"),
                    new DailySeed(27.1m, 28.3m, 52, 5.0m, "broken clouds", "04d"),
                    new DailySeed(26.4m, 27.0m, 55, 4.1m, "light rain", "10d")
                ]),
            ["gaborone"] = new(
                "Gaborone",
                "BW",
                -24.6282,
                25.9231,
                [
                    new DailySeed(30.1m, 31.3m, 37, 3.6m, "clear sky", "01d"),
                    new DailySeed(29.6m, 30.8m, 40, 3.9m, "few clouds", "02d"),
                    new DailySeed(28.8m, 29.9m, 45, 4.1m, "scattered clouds", "03d"),
                    new DailySeed(27.9m, 29.0m, 50, 4.7m, "broken clouds", "04d"),
                    new DailySeed(27.2m, 28.0m, 56, 4.3m, "light rain", "10d")
                ]),
            ["johannesburg"] = new(
                "Johannesburg",
                "ZA",
                -26.2041,
                28.0473,
                [
                    new DailySeed(24.3m, 24.9m, 49, 3.2m, "few clouds", "02d"),
                    new DailySeed(23.7m, 24.2m, 52, 3.4m, "scattered clouds", "03d"),
                    new DailySeed(22.8m, 23.5m, 57, 3.7m, "broken clouds", "04d"),
                    new DailySeed(21.9m, 22.6m, 61, 4.1m, "light rain", "10d"),
                    new DailySeed(22.5m, 23.0m, 54, 3.5m, "few clouds", "02d")
                ])
        };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly OpenWeatherMapOptions _options;

    public OpenWeatherMapClient(HttpClient httpClient, IMemoryCache cache, IOptions<OpenWeatherMapOptions> options)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<ExternalCurrentWeather> GetCurrentByCityAsync(
        string city,
        string? country,
        TemperatureUnit units,
        CancellationToken cancellationToken = default)
    {
        var normalizedCity = city.Trim();
        var normalizedCountry = string.IsNullOrWhiteSpace(country) ? string.Empty : country.Trim();

        var seededProfile = ResolveSeededProfile(normalizedCity, normalizedCountry);
        if (seededProfile is not null)
        {
            return BuildSeededCurrent(seededProfile, units);
        }

        var cacheKey = $"owm:current:{normalizedCity}:{normalizedCountry}:{units}";

        var current = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Max(1, _options.CacheDurationMinutes));
            var response = await SendAsync<CurrentWeatherApiResponse>(
                "data/2.5/weather",
                normalizedCity,
                normalizedCountry,
                units,
                cancellationToken);

            var weather = response.Weather.FirstOrDefault();
            return new ExternalCurrentWeather(
                response.Name,
                response.Sys.Country,
                response.Coord.Lat,
                response.Coord.Lon,
                Convert.ToDecimal(response.Main.Temp),
                Convert.ToDecimal(response.Main.FeelsLike),
                response.Main.Humidity,
                response.Main.Pressure,
                Convert.ToDecimal(response.Wind.Speed),
                weather?.Description ?? "Unknown",
                weather?.Icon ?? "01d",
                DateTimeOffset.FromUnixTimeSeconds(response.Dt).UtcDateTime,
                JsonSerializer.Serialize(response));
        });

        return current ?? throw new ExternalServiceException("OpenWeatherMap current weather response was empty.", 502);
    }

    public async Task<IReadOnlyList<ExternalForecastItem>> GetFiveDayForecastByCityAsync(
        string city,
        string? country,
        TemperatureUnit units,
        CancellationToken cancellationToken = default)
    {
        var normalizedCity = city.Trim();
        var normalizedCountry = string.IsNullOrWhiteSpace(country) ? string.Empty : country.Trim();

        var seededProfile = ResolveSeededProfile(normalizedCity, normalizedCountry);
        if (seededProfile is not null)
        {
            return BuildSeededForecast(seededProfile, units);
        }

        var cacheKey = $"owm:forecast:{normalizedCity}:{normalizedCountry}:{units}";

        var forecastItems = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Max(1, _options.CacheDurationMinutes));
            var response = await SendAsync<ForecastApiResponse>(
                "data/2.5/forecast",
                normalizedCity,
                normalizedCountry,
                units,
                cancellationToken);

            return response.List
                .Take(40)
                .Select(item =>
                {
                    var weather = item.Weather.FirstOrDefault();
                    return new ExternalForecastItem(
                        DateTimeOffset.FromUnixTimeSeconds(item.Dt).UtcDateTime,
                        Convert.ToDecimal(item.Main.Temp),
                        Convert.ToDecimal(item.Main.FeelsLike),
                        item.Main.Humidity,
                        Convert.ToDecimal(item.Wind.Speed),
                        weather?.Description ?? "Unknown",
                        weather?.Icon ?? "01d");
                })
                .ToList();
        });

        return forecastItems ?? [];
    }

    private async Task<T> SendAsync<T>(
        string path,
        string city,
        string country,
        TemperatureUnit units,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ExternalServiceException("OpenWeatherMap API key is not configured.", 500);
        }

        var queryCity = string.IsNullOrWhiteSpace(country) ? city : $"{city},{country}";
        var unitsQuery = units == TemperatureUnit.Metric ? "metric" : "imperial";
        var escapedCity = UrlEncoder.Default.Encode(queryCity);
        var uri = $"{path}?q={escapedCity}&appid={_options.ApiKey}&units={unitsQuery}";

        HttpResponseMessage response;
        string payload;

        try
        {
            response = await _httpClient.GetAsync(uri, cancellationToken);
            payload = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException("OpenWeatherMap request timed out.", 503, isTransient: true, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("OpenWeatherMap request failed due to network error.", 503, isTransient: true, ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var providerMessage = TryExtractProviderMessage(payload);

            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new ExternalServiceException(
                    providerMessage is null
                        ? "OpenWeatherMap authentication failed. Check API key validity and activation status."
                        : $"OpenWeatherMap authentication failed: {providerMessage}",
                    401),
                HttpStatusCode.NotFound => new ExternalServiceException("City not found.", 404),
                HttpStatusCode.TooManyRequests => new ExternalServiceException("OpenWeatherMap rate limit reached.", 429),
                _ => new ExternalServiceException(
                    providerMessage is null
                        ? $"OpenWeatherMap request failed ({(int)response.StatusCode})."
                        : $"OpenWeatherMap request failed ({(int)response.StatusCode}): {providerMessage}",
                    (int)response.StatusCode,
                    isTransient: (int)response.StatusCode >= 500)
            };
        }

        try
        {
            var result = JsonSerializer.Deserialize<T>(payload, JsonOptions);
            return result ?? throw new ExternalServiceException("OpenWeatherMap returned empty payload.", 502);
        }
        catch (JsonException ex)
        {
            throw new ExternalServiceException("OpenWeatherMap response could not be parsed.", 502, isTransient: true, ex);
        }
    }

    private static string? TryExtractProviderMessage(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.TryGetProperty("message", out var messageProperty) &&
                messageProperty.ValueKind == JsonValueKind.String)
            {
                return messageProperty.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static SeededCityProfile? ResolveSeededProfile(string city, string country)
    {
        if (!SeededProfiles.TryGetValue(city, out var profile))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            return profile;
        }

        return string.Equals(country.Trim(), profile.Country, StringComparison.OrdinalIgnoreCase)
            ? profile
            : null;
    }

    private static ExternalCurrentWeather BuildSeededCurrent(SeededCityProfile profile, TemperatureUnit units)
    {
        var now = DateTime.UtcNow;
        var baseDay = profile.Days[0];
        var cycle = Math.Sin(now.TimeOfDay.TotalHours / 24d * Math.PI * 2d);
        var variation = Convert.ToDecimal(cycle * 1.9d);

        var temperatureMetric = decimal.Round(baseDay.TemperatureC + variation, 2);
        var feelsMetric = decimal.Round(baseDay.FeelsLikeC + variation * 0.82m, 2);
        var windMetric = decimal.Round(Math.Max(0.6m, baseDay.WindMs + Convert.ToDecimal(Math.Cos(cycle) * 0.7d)), 2);
        var humidity = Math.Clamp(baseDay.Humidity - (int)Math.Round(cycle * 6d), 32, 98);

        var payload = JsonSerializer.Serialize(new
        {
            provider = "seeded",
            profile.City,
            profile.Country,
            generatedAtUtc = now
        });

        return new ExternalCurrentWeather(
            profile.City,
            profile.Country,
            profile.Latitude,
            profile.Longitude,
            ConvertTemperature(temperatureMetric, units),
            ConvertTemperature(feelsMetric, units),
            humidity,
            1013,
            ConvertWind(windMetric, units),
            baseDay.Summary,
            baseDay.IconCode,
            now,
            payload);
    }

    private static IReadOnlyList<ExternalForecastItem> BuildSeededForecast(SeededCityProfile profile, TemperatureUnit units)
    {
        var start = DateTime.UtcNow.Date;
        var items = new List<ExternalForecastItem>(40);

        for (var dayIndex = 0; dayIndex < 5; dayIndex++)
        {
            var daySeed = profile.Days[Math.Min(dayIndex, profile.Days.Count - 1)];

            for (var slot = 0; slot < 8; slot++)
            {
                var point = start.AddDays(dayIndex).AddHours(slot * 3);
                var cycle = Math.Sin(((slot - 1) / 8d) * Math.PI * 2d);
                var tempMetric = decimal.Round(daySeed.TemperatureC + Convert.ToDecimal(cycle * 2.4d), 2);
                var feelsMetric = decimal.Round(daySeed.FeelsLikeC + Convert.ToDecimal(cycle * 2.1d), 2);
                var humidity = Math.Clamp(daySeed.Humidity - (int)Math.Round(cycle * 8d), 32, 98);
                var windMetric = decimal.Round(
                    Math.Max(0.5m, daySeed.WindMs + Convert.ToDecimal(Math.Cos((slot + 1) / 8d * Math.PI * 2d) * 1.1d)),
                    2);

                items.Add(new ExternalForecastItem(
                    point,
                    ConvertTemperature(tempMetric, units),
                    ConvertTemperature(feelsMetric, units),
                    humidity,
                    ConvertWind(windMetric, units),
                    daySeed.Summary,
                    daySeed.IconCode));
            }
        }

        return items;
    }

    private static decimal ConvertTemperature(decimal temperatureCelsius, TemperatureUnit units)
    {
        return units == TemperatureUnit.Imperial
            ? decimal.Round((temperatureCelsius * 9m / 5m) + 32m, 2)
            : decimal.Round(temperatureCelsius, 2);
    }

    private static decimal ConvertWind(decimal windMetersPerSecond, TemperatureUnit units)
    {
        return units == TemperatureUnit.Imperial
            ? decimal.Round(windMetersPerSecond * 2.23694m, 2)
            : decimal.Round(windMetersPerSecond, 2);
    }

    private sealed record DailySeed(
        decimal TemperatureC,
        decimal FeelsLikeC,
        int Humidity,
        decimal WindMs,
        string Summary,
        string IconCode);

    private sealed record SeededCityProfile(
        string City,
        string Country,
        double Latitude,
        double Longitude,
        IReadOnlyList<DailySeed> Days);

    private sealed class CurrentWeatherApiResponse
    {
        [JsonPropertyName("coord")]
        public Coord Coord { get; set; } = new();

        [JsonPropertyName("weather")]
        public List<WeatherData> Weather { get; set; } = [];

        [JsonPropertyName("main")]
        public MainData Main { get; set; } = new();

        [JsonPropertyName("wind")]
        public WindData Wind { get; set; } = new();

        [JsonPropertyName("sys")]
        public SysData Sys { get; set; } = new();

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("dt")]
        public long Dt { get; set; }
    }

    private sealed class ForecastApiResponse
    {
        [JsonPropertyName("list")]
        public List<ForecastItemData> List { get; set; } = [];
    }

    private sealed class ForecastItemData
    {
        [JsonPropertyName("dt")]
        public long Dt { get; set; }

        [JsonPropertyName("main")]
        public MainData Main { get; set; } = new();

        [JsonPropertyName("wind")]
        public WindData Wind { get; set; } = new();

        [JsonPropertyName("weather")]
        public List<WeatherData> Weather { get; set; } = [];
    }

    private sealed class Coord
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }
    }

    private sealed class MainData
    {
        [JsonPropertyName("temp")]
        public double Temp { get; set; }

        [JsonPropertyName("feels_like")]
        public double FeelsLike { get; set; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }

        [JsonPropertyName("pressure")]
        public int Pressure { get; set; }
    }

    private sealed class WindData
    {
        [JsonPropertyName("speed")]
        public double Speed { get; set; }
    }

    private sealed class WeatherData
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;
    }

    private sealed class SysData
    {
        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;
    }
}
