using Homevault.Domain.Entities;

namespace Homevault.Application.Ports;

public interface IWeatherObservationRepository
{
    Task AddAsync(
        WeatherObservation observation,
        CancellationToken cancellationToken = default);

    Task<int> DeleteOlderThanAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken = default);

    Task<WeatherObservation?> GetLatestCollectedAsync(
        string city,
        string state,
        CancellationToken cancellationToken = default);

    Task<WeatherObservation?> GetLatestAsync(
        string city,
        string state,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeatherObservation>> GetHistoryAsync(
        string city,
        string state,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
