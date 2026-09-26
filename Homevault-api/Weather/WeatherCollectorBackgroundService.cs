using Homevault.Application.Ports;
using Homevault.Application.Weather;
using Homevault.Infrastructure.Weather;
using Microsoft.Extensions.Options;

namespace Homevault_api.Weather;

public sealed class WeatherCollectorBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<WeatherOptions> options,
    ILogger<WeatherCollectorBackgroundService> logger) : BackgroundService
{
    private DateTimeOffset nextRetentionCleanup = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(options.Value.CollectionIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CollectAsync(stoppingToken);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task CollectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var collector = scope.ServiceProvider.GetRequiredService<CollectWeather>();
            var repository = scope.ServiceProvider
                .GetRequiredService<IWeatherObservationRepository>();
            var location = CreateLocation(options.Value.Location);

            var latestCollected = await repository.GetLatestCollectedAsync(
                location.City,
                location.State,
                cancellationToken);
            var interval = TimeSpan.FromMinutes(options.Value.CollectionIntervalMinutes);
            var now = DateTimeOffset.UtcNow;

            if (latestCollected is not null &&
                latestCollected.CollectedAt.Add(interval) > now)
            {
                logger.LogInformation(
                    "Coleta ignorada para {City}/{State}: o último registro foi coletado em {CollectedAt} e o próximo estará disponível em {NextCollectionAt}",
                    location.City,
                    location.State,
                    latestCollected.CollectedAt,
                    latestCollected.CollectedAt.Add(interval));
                return;
            }

            var observation = await collector.ExecuteAsync(location, cancellationToken);

            logger.LogInformation(
                "Clima coletado para {City}/{State}: {Temperature} °C e {Humidity}% em {ObservedAt}",
                observation.City,
                observation.State,
                observation.TemperatureCelsius,
                observation.RelativeHumidity,
                observation.ObservedAt);

            if (DateTimeOffset.UtcNow >= nextRetentionCleanup)
            {
                var cutoff = DateTimeOffset.UtcNow.AddMonths(-options.Value.RetentionMonths);
                var deletedCount = await repository.DeleteOlderThanAsync(
                    cutoff,
                    cancellationToken);

                nextRetentionCleanup = DateTimeOffset.UtcNow.AddDays(1);

                logger.LogInformation(
                    "Retenção do histórico executada. {DeletedCount} observações removidas anteriores a {Cutoff}",
                    deletedCount,
                    cutoff);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao coletar o clima atual");
        }
    }

    private static WeatherLocation CreateLocation(LocationOptions options)
    {
        return new WeatherLocation(
            options.City,
            options.State,
            options.Latitude,
            options.Longitude,
            options.TimeZone);
    }
}
