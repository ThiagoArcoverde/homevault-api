using Homevault.Application.Ports;
using Homevault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Homevault.Infrastructure.Persistence;

public sealed class HomeRepository(HomeDbContext dbContext) : IHomeRepository
{
    public async Task AddAsync(Home home, CancellationToken cancellationToken = default)
    {
        await dbContext.Homes.AddAsync(home, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Home?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Homes
            .AsNoTracking()
            .SingleOrDefaultAsync(home => home.Id == id, cancellationToken);
    }
}
