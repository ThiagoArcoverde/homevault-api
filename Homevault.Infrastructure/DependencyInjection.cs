using Homevault.Application.Ports;
using Homevault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
