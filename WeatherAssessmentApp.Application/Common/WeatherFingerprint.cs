using System.Security.Cryptography;
using System.Text;
using WeatherAssessmentApp.Application.Models;

namespace WeatherAssessmentApp.Application.Common;

internal static class WeatherFingerprint
{
    public static string From(ExternalCurrentWeather weather)
    {
        var raw = string.Join(
            '|',
            weather.Temperature,
            weather.FeelsLike,
            weather.Humidity,
            weather.Pressure,
            weather.WindSpeed,
            weather.Summary,
            weather.IconCode,
            weather.ObservedAtUtc.ToUniversalTime().ToString("O"));

        var bytes = Encoding.UTF8.GetBytes(raw);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
