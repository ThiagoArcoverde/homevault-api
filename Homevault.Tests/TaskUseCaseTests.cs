using Homevault.Application.Ports;
using Homevault.Application.Tasks;
using Homevault.Domain.Entities;
using Xunit;

namespace Homevault.Tests;

public sealed class TaskUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    [Fact]
    public void Domain_Trims_Defaults_And_Updates_Only_Provided_Fields()
    {
        var task = new TodoTask(Guid.NewGuid(), "  Limpar  ");
        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal("Limpar", task.Title);
        Assert.Equal("Sem prazo", task.Meta);
        Assert.Equal("Todos", task.Assignee);
        Assert.False(task.Done);
        task.Update(null, "  Hoje  ", "  Casa  ", true, Now, TimeZone);
        Assert.Equal("Limpar", task.Title);
        Assert.Equal("Hoje", task.Meta);
        Assert.Equal("Casa", task.Assignee);
        Assert.True(task.Done);
        task.Update("  Organizar  ", null, null, false, Now, TimeZone);
        Assert.Equal("Organizar", task.Title);
        Assert.Equal("Hoje", task.Meta);
        Assert.Equal("Casa", task.Assignee);
        Assert.False(task.Done);
        Assert.Throws<ArgumentException>(() => task.Update("Alterado", " ", null, true, Now, TimeZone));
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
        Assert.Throws<ArgumentException>(() => task.Update(titleText, metaText, assigneeText, null, Now, TimeZone));
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
        var tasks = new HomeTasks(repository, Guid.NewGuid(), new TestTimeProvider(Now), TimeZone);
        var otherTasks = new HomeTasks(repository, Guid.NewGuid(), new TestTimeProvider(Now), TimeZone);
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

    [Theory]
    [InlineData("2026-10-21T02:59:59Z", "2026-10-21T03:00:00Z", "America/Sao_Paulo")]
    [InlineData("2026-10-20T23:59:59Z", "2026-10-21T00:00:00Z", "UTC")]
    // Sao Paulo skipped midnight when DST began, and repeated 23:00 when it ended.
    [InlineData("2018-11-04T02:59:59Z", "2018-11-04T03:00:00Z", "America/Sao_Paulo")]
    [InlineData("2019-02-17T02:59:59Z", "2019-02-17T03:00:00Z", "America/Sao_Paulo")]
    public async Task Daily_Completion_Expires_On_Local_Date_Change_Not_After_24_Hours(
        string before, string after, string zone)
    {
        var clock = new TestTimeProvider(DateTimeOffset.Parse(before));
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(zone);
        var repository = new FakeTaskRepository();
        var tasks = new HomeTasks(repository, Guid.NewGuid(), clock, timeZone);
        var daily = await tasks.CreateAsync("Diaria", null, null, default, repeatsDaily: true);
        var persistent = await tasks.CreateAsync("Permanente", null, null, default);
        Assert.True(daily.RepeatsDaily);
        Assert.False(daily.Done);
        Assert.True((await tasks.UpdateAsync(daily.Id, new TaskPatch(null, null, null, true), default))!.Done);
        await tasks.UpdateAsync(persistent.Id, new TaskPatch(null, null, null, true), default);
        var stored = repository.Items.Single(task => task.Id == daily.Id);
        Assert.Equal(clock.UtcNow, stored.CompletedAtUtc);
        var saved = repository.SaveCount;
        clock.UtcNow = DateTimeOffset.Parse(after);
        Assert.NotEqual(TimeZoneInfo.ConvertTime(DateTimeOffset.Parse(before), timeZone).Date,
            TimeZoneInfo.ConvertTime(clock.UtcNow, timeZone).Date);
        var listed = (await tasks.ListAsync(default)).Items;
        Assert.False(listed.Single(task => task.Id == daily.Id).Done);
        Assert.True(listed.Single(task => task.Id == persistent.Id).Done);
        Assert.Equal(saved, repository.SaveCount);
        Assert.Equal(DateTimeOffset.Parse(before), stored.CompletedAtUtc);
        Assert.True((await tasks.UpdateAsync(daily.Id, new TaskPatch(null, null, null, true), default))!.Done);
        Assert.Equal(clock.UtcNow, stored.CompletedAtUtc);
        Assert.False((await tasks.UpdateAsync(daily.Id, new TaskPatch(null, null, null, false), default))!.Done);
        Assert.Null(stored.CompletedAtUtc);
    }

    [Theory]
    [InlineData("2026-10-20T23:59:59Z", "2026-10-21T00:00:00Z")]
    [InlineData("2019-02-17T01:59:59Z", "2019-02-17T02:00:00Z")]
    public async Task Utc_Midnight_And_Dst_Repeated_Hour_Do_Not_Reset_The_Same_Local_Date(string before, string after)
    {
        var clock = new TestTimeProvider(DateTimeOffset.Parse(before));
        var tasks = new HomeTasks(new FakeTaskRepository(), Guid.NewGuid(), clock, TimeZone);
        var daily = await tasks.CreateAsync("Diaria", null, null, default, repeatsDaily: true);
        await tasks.UpdateAsync(daily.Id, new TaskPatch(null, null, null, true), default);
        clock.UtcNow = DateTimeOffset.Parse(after);
        Assert.True((await tasks.ListAsync(default)).Items.Single().Done);
    }

    [Fact]
    public async Task Changing_Recurrence_Preserves_Visible_State_And_Explicit_Done_Takes_Precedence()
    {
        var clock = new TestTimeProvider(Now);
        var repository = new FakeTaskRepository();
        var tasks = new HomeTasks(repository, Guid.NewGuid(), clock, TimeZone);
        var created = await tasks.CreateAsync("Afazer", null, null, default);
        var id = created.Id;
        var stored = repository.Items.Single();
        await tasks.UpdateAsync(id, new TaskPatch(null, null, null, true), default);
        clock.UtcNow = Now.AddDays(7);

        // Enabling daily recurrence for an old persistent completion keeps it done today.
        var updated = await tasks.UpdateAsync(id, new TaskPatch(null, null, null, null, true), default);
        Assert.True(updated!.Done);
        Assert.True(updated.RepeatsDaily);
        Assert.Equal(clock.UtcNow, stored.CompletedAtUtc);
        clock.UtcNow = clock.UtcNow.AddHours(1);
        var completed = stored.CompletedAtUtc;
        await tasks.UpdateAsync(id, new TaskPatch(null, null, null, null, true), default);
        Assert.Equal(completed, stored.CompletedAtUtc);

        // Turning off an active daily completion preserves it indefinitely.
        updated = await tasks.UpdateAsync(id, new TaskPatch(null, null, null, null, false), default);
        Assert.True(updated!.Done);
        Assert.Equal(completed, stored.CompletedAtUtc);
        clock.UtcNow = clock.UtcNow.AddDays(1);
        Assert.True((await tasks.ListAsync(default)).Items.Single().Done);
        await tasks.UpdateAsync(id, new TaskPatch(null, null, null, null, true), default);
        clock.UtcNow = clock.UtcNow.AddDays(1);

        // Turning off an expired daily task must not resurrect yesterday's completion.
        updated = await tasks.UpdateAsync(id, new TaskPatch(null, null, null, null, false), default);
        Assert.False(updated!.Done);
        Assert.Null(stored.CompletedAtUtc);
        updated = await tasks.UpdateAsync(id, new TaskPatch(null, null, null, null, true), default);
        Assert.False(updated!.Done);
        Assert.Null(stored.CompletedAtUtc);

        updated = await tasks.UpdateAsync(id, new TaskPatch(null, null, null, true, false), default);
        Assert.True(updated!.Done);
        Assert.False(updated.RepeatsDaily);
        updated = await tasks.UpdateAsync(id, new TaskPatch(null, null, null, false, true), default);
        Assert.False(updated!.Done);
        Assert.True(updated.RepeatsDaily);
        Assert.Null(stored.CompletedAtUtc);
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        private readonly List<TodoTask> tasks = [];
        public int SaveCount { get; private set; }
        public IReadOnlyList<TodoTask> Items => tasks;

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
