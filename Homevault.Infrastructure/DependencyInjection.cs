using Homevault.Application.Ports;
using Homevault.Infrastructure.Persistence;
using Homevault.Infrastructure.Weather;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Homevault.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<HomeDbContext>(options =>
            options.UseSqlite(connectionString));
        services.AddScoped<IHomeRepository, HomeRepository>();
        services.AddScoped<IWeatherObservationRepository, WeatherObservationRepository>();
        services.AddHttpClient<IWeatherProvider, OpenMeteoWeatherProvider>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<WeatherOptions>>().Value;
            client.BaseAddress = new Uri(options.ProviderBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });

        return services;
    }
}
