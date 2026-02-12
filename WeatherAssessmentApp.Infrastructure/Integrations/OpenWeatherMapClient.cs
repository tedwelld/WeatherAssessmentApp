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
