using Homevault.Domain.Entities;

namespace Homevault.Application.Ports;

public interface ITaskRepository
{
    Task<IReadOnlyList<TodoTask>> GetTasksAsync(Guid homeId, CancellationToken token);
    Task<TodoTask?> GetTaskAsync(Guid homeId, Guid id, CancellationToken token);
    Task AddAsync(TodoTask task, CancellationToken token);
    Task SaveAsync(CancellationToken token);
    Task DeleteAsync(TodoTask task, CancellationToken token);
}
