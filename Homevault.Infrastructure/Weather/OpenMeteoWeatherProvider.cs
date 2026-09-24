using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Homevault.Application.Ports;

namespace Homevault.Infrastructure.Weather;

public sealed class OpenMeteoWeatherProvider(
    HttpClient httpClient) : IWeatherProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<WeatherReading> GetCurrentAsync(
        WeatherLocation location,
        CancellationToken cancellationToken = default)
    {
        var query = $"forecast?latitude={location.Latitude.ToString(CultureInfo.InvariantCulture)}" +
                    $"&longitude={location.Longitude.ToString(CultureInfo.InvariantCulture)}" +
                    "&current=temperature_2m,relative_humidity_2m" +
                    $"&timezone={Uri.EscapeDataString(location.TimeZone)}";

        using var response = await httpClient.GetAsync(query, cancellationToken);
        response.EnsureSuccessStatusCode();

        var weather = await response.Content.ReadFromJsonAsync<OpenMeteoResponse>(
            JsonOptions,
            cancellationToken);

        if (weather?.Current is null)
        {
            throw new InvalidOperationException("A resposta do provedor não contém dados atuais.");
        }

        var observedAt = ParseObservedAt(weather.Current.Time, location.TimeZone);

        return new WeatherReading(
            weather.Current.Temperature,
            weather.Current.RelativeHumidity,
            observedAt);
    }

    private static DateTimeOffset ParseObservedAt(string value, string timeZoneId)
    {
        if (!DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var localDateTime))
        {
            throw new InvalidOperationException("O horário retornado pelo provedor é inválido.");
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var unspecifiedDateTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(unspecifiedDateTime, timeZone);

        return new DateTimeOffset(utcDateTime);
    }

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("current")]
        public CurrentWeather? Current { get; init; }
    }

    private sealed class CurrentWeather
    {
        [JsonPropertyName("temperature_2m")]
        public decimal Temperature { get; init; }

        [JsonPropertyName("relative_humidity_2m")]
        public decimal RelativeHumidity { get; init; }

        [JsonPropertyName("time")]
        public string Time { get; init; } = string.Empty;
    }
}
