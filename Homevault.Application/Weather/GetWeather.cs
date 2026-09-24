using Homevault.Application.Ports;
using Homevault.Domain.Entities;

namespace Homevault.Application.Weather;

public sealed class GetWeather(IWeatherObservationRepository observationRepository)
{
    public Task<WeatherObservation?> GetLatestAsync(
        WeatherLocation location,
        CancellationToken cancellationToken = default)
    {
        return observationRepository.GetLatestAsync(
            location.City,
            location.State,
            cancellationToken);
    }

    public Task<IReadOnlyList<WeatherObservation>> GetHistoryAsync(
        WeatherLocation location,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return observationRepository.GetHistoryAsync(
            location.City,
            location.State,
            from,
            to,
            page,
            pageSize,
            cancellationToken);
    }
}
