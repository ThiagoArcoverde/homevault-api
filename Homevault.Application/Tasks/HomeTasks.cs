using Homevault.Application.Ports;
using Homevault.Domain.Entities;

namespace Homevault.Application.Tasks;

public sealed record TaskView(Guid Id, string Title, string Meta, string Assignee, bool Done)
{
    public static TaskView From(TodoTask task) => new(task.Id, task.Title, task.Meta, task.Assignee, task.Done);
}

public sealed record TasksResponse(IReadOnlyList<TaskView> Items);
public sealed record TaskPatch(string? Title, string? Meta, string? Assignee, bool? Done);

public sealed class HomeTasks(ITaskRepository repository, Guid homeId)
{
    public async Task<TasksResponse> ListAsync(CancellationToken token) =>
        new((await repository.GetTasksAsync(homeId, token)).Select(TaskView.From).ToArray());

    public async Task<TaskView> CreateAsync(string title, string? meta, string? assignee, CancellationToken token)
    {
        var task = new TodoTask(homeId, title, meta, assignee);
        await repository.AddAsync(task, token);
        return TaskView.From(task);
    }

    public async Task<TaskView?> UpdateAsync(Guid id, TaskPatch patch, CancellationToken token)
    {
        var task = await repository.GetTaskAsync(homeId, id, token);
        if (task is null) return null;
        task.Update(patch.Title, patch.Meta, patch.Assignee, patch.Done);
        await repository.SaveAsync(token);
        return TaskView.From(task);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken token)
    {
        var task = await repository.GetTaskAsync(homeId, id, token);
        if (task is null) return false;
        await repository.DeleteAsync(task, token);
        return true;
    }
}
