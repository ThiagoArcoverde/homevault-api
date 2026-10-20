using Homevault.Application.Ports;
using Homevault.Domain.Entities;

namespace Homevault.Application.Tasks;

public sealed record TaskView(Guid Id, string Title, string Meta, string Assignee, bool Done, bool RepeatsDaily)
{
    public static TaskView From(TodoTask task, DateTimeOffset now, TimeZoneInfo timeZone) =>
        new(task.Id, task.Title, task.Meta, task.Assignee, task.IsDone(now, timeZone), task.RepeatsDaily);
}

public sealed record TasksResponse(IReadOnlyList<TaskView> Items);
public sealed record TaskPatch(string? Title, string? Meta, string? Assignee, bool? Done, bool? RepeatsDaily = null);

public sealed class HomeTasks(ITaskRepository repository, Guid homeId, TimeProvider timeProvider, TimeZoneInfo timeZone)
{
    public async Task<TasksResponse> ListAsync(CancellationToken token)
    {
        var tasks = await repository.GetTasksAsync(homeId, token);
        var now = timeProvider.GetUtcNow();
        return new(tasks.Select(task => TaskView.From(task, now, timeZone)).ToArray());
    }

    public async Task<TaskView> CreateAsync(string title, string? meta, string? assignee, CancellationToken token,
        bool repeatsDaily = false)
    {
        var task = new TodoTask(homeId, title, meta, assignee, repeatsDaily);
        await repository.AddAsync(task, token);
        return TaskView.From(task, timeProvider.GetUtcNow(), timeZone);
    }

    public async Task<TaskView?> UpdateAsync(Guid id, TaskPatch patch, CancellationToken token)
    {
        var task = await repository.GetTaskAsync(homeId, id, token);
        if (task is null) return null;
        var now = timeProvider.GetUtcNow();
        task.Update(patch.Title, patch.Meta, patch.Assignee, patch.Done, now, timeZone, patch.RepeatsDaily);
        await repository.SaveAsync(token);
        return TaskView.From(task, timeProvider.GetUtcNow(), timeZone);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken token)
    {
        var task = await repository.GetTaskAsync(homeId, id, token);
        if (task is null) return false;
        await repository.DeleteAsync(task, token);
        return true;
    }
}
