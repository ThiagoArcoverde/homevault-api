using Homevault.Domain.Entities;

namespace Homevault.Application.Ports;

public interface IHomeRepository
{
    Task AddAsync(Home home, CancellationToken cancellationToken = default);

    Task<Home?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
