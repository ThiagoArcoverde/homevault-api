using Homevault.Application.Ports;
using Homevault.Domain.Entities;

namespace Homevault.Application.Weather;

public sealed class CollectWeather(
    IWeatherProvider weatherProvider,
    IWeatherObservationRepository observationRepository)
{
    public async Task<WeatherObservation> ExecuteAsync(
        WeatherLocation location,
        CancellationToken cancellationToken = default)
    {
        var reading = await weatherProvider.GetCurrentAsync(location, cancellationToken);
        var observation = new WeatherObservation(
            location.City,
            location.State,
            location.Latitude,
            location.Longitude,
            reading.TemperatureCelsius,
            reading.RelativeHumidity,
            reading.ObservedAt,
            DateTimeOffset.UtcNow);

        await observationRepository.AddAsync(observation, cancellationToken);
        return observation;
    }
}
