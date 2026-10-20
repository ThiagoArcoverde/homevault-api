using Homevault.Application.Ports;
using Homevault.Application.Tasks;
using Homevault.Domain.Entities;
using Xunit;

namespace Homevault.Tests;

public sealed class TaskUseCaseTests
{
    [Fact]
    public void Domain_Trims_Defaults_And_Updates_Only_Provided_Fields()
    {
        var task = new TodoTask(Guid.NewGuid(), "  Limpar  ");
        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal("Limpar", task.Title);
        Assert.Equal("Sem prazo", task.Meta);
        Assert.Equal("Todos", task.Assignee);
        Assert.False(task.Done);
        task.Update(null, "  Hoje  ", "  Casa  ", true);
        Assert.Equal("Limpar", task.Title);
        Assert.Equal("Hoje", task.Meta);
        Assert.Equal("Casa", task.Assignee);
        Assert.True(task.Done);
        task.Update("  Organizar  ", null, null, false);
        Assert.Equal("Organizar", task.Title);
        Assert.Equal("Hoje", task.Meta);
        Assert.Equal("Casa", task.Assignee);
        Assert.False(task.Done);
        Assert.Throws<ArgumentException>(() => task.Update("Alterado", " ", null, true));
        Assert.Equal("Organizar", task.Title);
        Assert.False(task.Done);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(121, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 61, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, 41)]
    public void Domain_Rejects_Invalid_Lengths_On_Create_And_Update(int title, int meta, int assignee)
    {
        var task = new TodoTask(Guid.NewGuid(), "Inicial");
        var titleText = new string('x', title);
        var metaText = new string('x', meta);
        var assigneeText = new string('x', assignee);
        Assert.Throws<ArgumentException>(() => new TodoTask(task.HomeId, titleText, metaText, assigneeText));
        Assert.Throws<ArgumentException>(() => task.Update(titleText, metaText, assigneeText, null));
        Assert.Equal("Inicial", task.Title);
    }

    [Fact]
    public void Domain_Accepts_Exact_Maximum_Lengths_After_Trim()
    {
        var task = new TodoTask(Guid.NewGuid(), $" {new string('t', 120)} ", $" {new string('m', 60)} ",
            $" {new string('a', 40)} ");
        Assert.Equal(120, task.Title.Length);
        Assert.Equal(60, task.Meta.Length);
        Assert.Equal(40, task.Assignee.Length);
    }

    [Fact]
    public async Task UseCase_Creates_Lists_Updates_Deletes_And_Isolates_Homes()
    {
        var repository = new FakeTaskRepository();
        var tasks = new HomeTasks(repository, Guid.NewGuid());
        var otherTasks = new HomeTasks(repository, Guid.NewGuid());
        var first = await tasks.CreateAsync("  Primeiro  ", null, null, default);
        var second = await tasks.CreateAsync("Segundo", " Hoje ", " Alex ", default);
        var foreign = await otherTasks.CreateAsync("Privado", null, null, default);
        Assert.Equal("Primeiro", first.Title);
        Assert.Equal("Sem prazo", first.Meta);
        Assert.Equal("Todos", first.Assignee);
        Assert.False(first.Done);
        Assert.Equal(new[] { first.Id, second.Id }, (await tasks.ListAsync(default)).Items.Select(task => task.Id));
        Assert.Null(await tasks.UpdateAsync(foreign.Id, new TaskPatch(null, null, null, true), default));
        Assert.False(await tasks.DeleteAsync(foreign.Id, default));
        Assert.Null(await tasks.UpdateAsync(Guid.NewGuid(), new TaskPatch("Ausente", null, null, null), default));
        var updated = await tasks.UpdateAsync(first.Id, new TaskPatch(null, null, null, true), default);
        Assert.Equal(first with { Done = true }, updated);
        Assert.Equal(1, repository.SaveCount);
        Assert.True(await tasks.DeleteAsync(first.Id, default));
        Assert.False(await tasks.DeleteAsync(first.Id, default));
        Assert.Equal(second.Id, (await tasks.ListAsync(default)).Items.Single().Id);
        Assert.Equal(foreign.Id, (await otherTasks.ListAsync(default)).Items.Single().Id);
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        private readonly List<TodoTask> tasks = [];
        public int SaveCount { get; private set; }

        public Task<IReadOnlyList<TodoTask>> GetTasksAsync(Guid homeId, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<TodoTask>>(tasks.Where(task => task.HomeId == homeId).ToArray());

        public Task<TodoTask?> GetTaskAsync(Guid homeId, Guid id, CancellationToken token) =>
            Task.FromResult(tasks.FirstOrDefault(task => task.HomeId == homeId && task.Id == id));

        public Task AddAsync(TodoTask task, CancellationToken token)
        {
            tasks.Add(task);
            return Task.CompletedTask;
        }

        public Task SaveAsync(CancellationToken token)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TodoTask task, CancellationToken token)
        {
            tasks.Remove(task);
            return Task.CompletedTask;
        }
    }
}
