using Homevault.Application.Ports;
using Homevault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Homevault.Infrastructure.Persistence;

public sealed class WeatherObservationRepository(HomeDbContext dbContext)
    : IWeatherObservationRepository
{
    public async Task AddAsync(
        WeatherObservation observation,
        CancellationToken cancellationToken = default)
    {
        await dbContext.WeatherObservations.AddAsync(observation, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<int> DeleteOlderThanAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken = default)
    {
        return dbContext.WeatherObservations
            .Where(observation => observation.CollectedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task<WeatherObservation?> GetLatestCollectedAsync(
        string city,
        string state,
        CancellationToken cancellationToken = default)
    {
        return dbContext.WeatherObservations
            .AsNoTracking()
            .Where(observation => observation.City == city && observation.State == state)
            .OrderByDescending(observation => observation.CollectedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<WeatherObservation?> GetLatestAsync(
        string city,
        string state,
        CancellationToken cancellationToken = default)
    {
        return dbContext.WeatherObservations
            .AsNoTracking()
            .Where(observation => observation.City == city && observation.State == state)
            .OrderByDescending(observation => observation.ObservedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WeatherObservation>> GetHistoryAsync(
        string city,
        string state,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WeatherObservations
            .AsNoTracking()
            .Where(observation => observation.City == city && observation.State == state);

        if (from.HasValue)
        {
            query = query.Where(observation => observation.ObservedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(observation => observation.ObservedAt <= to.Value);
        }

        return await query
            .OrderByDescending(observation => observation.ObservedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
