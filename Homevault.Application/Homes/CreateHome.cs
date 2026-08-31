using Homevault.Application.Ports;
using Homevault.Domain.Entities;

namespace Homevault.Application.Homes;

public sealed record CreateHomeCommand(string Name);

public sealed class CreateHome(IHomeRepository homeRepository)
{
    public async Task<Home> ExecuteAsync(
        CreateHomeCommand command,
        CancellationToken cancellationToken = default)
    {
        var home = new Home(command.Name);
        await homeRepository.AddAsync(home, cancellationToken);
        return home;
    }
}
