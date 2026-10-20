using Homevault.Application.Ports;
using Homevault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Homevault.Infrastructure.Persistence;

public sealed class TaskRepository(HomeDbContext context) : ITaskRepository
{
    public async Task<IReadOnlyList<TodoTask>> GetTasksAsync(Guid homeId, CancellationToken token) =>
        await context.HomeTasks.AsNoTracking().Where(task => task.HomeId == homeId)
            .OrderBy(task => task.SortOrder).ToListAsync(token);

    public Task<TodoTask?> GetTaskAsync(Guid homeId, Guid id, CancellationToken token) =>
        context.HomeTasks.FirstOrDefaultAsync(task => task.HomeId == homeId && task.Id == id, token);

    public async Task AddAsync(TodoTask task, CancellationToken token)
    {
        // SQLite's immediate write transaction serializes allocation of the next position.
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var lastOrder = await context.HomeTasks.Where(item => item.HomeId == task.HomeId)
            .MaxAsync(item => (long?)item.SortOrder, token) ?? 0;
        context.HomeTasks.Add(task);
        context.Entry(task).Property(item => item.SortOrder).CurrentValue = checked(lastOrder + 1);
        await context.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    public Task SaveAsync(CancellationToken token) => context.SaveChangesAsync(token);

    public async Task DeleteAsync(TodoTask task, CancellationToken token)
    {
        context.HomeTasks.Remove(task);
        await context.SaveChangesAsync(token);
    }
}
