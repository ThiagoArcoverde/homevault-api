namespace Homevault.Application.Ports;

public interface IWeatherProvider
{
    Task<WeatherReading> GetCurrentAsync(
        WeatherLocation location,
        CancellationToken cancellationToken = default);
}

public sealed record WeatherLocation(
    string City,
    string State,
    decimal Latitude,
    decimal Longitude,
    string TimeZone);

public sealed record WeatherReading(
    decimal TemperatureCelsius,
    decimal RelativeHumidity,
    DateTimeOffset ObservedAt);
