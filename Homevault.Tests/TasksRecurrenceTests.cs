using System.Net;
using System.Net.Http.Json;
using System.Text;
using Homevault.Application.Tasks;
using Homevault.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Homevault.Tests;

public sealed partial class TasksTests
{
    [Theory]
    [InlineData("America/Sao_Paulo", "2026-10-21T02:59:59Z", "2026-10-21T03:00:00Z")]
    [InlineData("UTC", "2026-10-20T23:59:59Z", "2026-10-21T00:00:00Z")]
    [InlineData("America/Sao_Paulo", "2018-11-04T02:59:59Z", "2018-11-04T03:00:00Z")]
    public async Task Http_Recurrence_Uses_Configured_TimeZone_And_Persists_Across_Restart(
        string timeZone, string before, string after)
    {
        var database = Path.Combine(Path.GetTempPath(), $"homevault-recurrence-{Guid.NewGuid()}.db");
        var clock = new TestTimeProvider(DateTimeOffset.Parse(before));
        try
        {
            Guid id;
            Guid persistentId;
            using (var factory = new ApiFactory(database, clock, timeZone))
            using (var client = factory.CreateClient())
            {
                var response = await client.PostAsJsonAsync(BaseUrl, new { title = "Diaria", repeatsDaily = true });
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                var daily = (await response.Content.ReadFromJsonAsync<TaskView>())!;
                id = daily.Id;
                Assert.True(daily.RepeatsDaily);
                Assert.False(daily.Done);
                Assert.Equal($"{BaseUrl}/{id}", response.Headers.Location?.ToString());
                var persistent = await client.PostAsJsonAsync(BaseUrl, new { title = "Persistente" });
                persistentId = (await persistent.Content.ReadFromJsonAsync<TaskView>())!.Id;
                Assert.Equal(HttpStatusCode.OK,
                    (await client.PatchAsJsonAsync($"{BaseUrl}/{persistentId}", new { done = true })).StatusCode);
                response = await client.PatchAsJsonAsync($"{BaseUrl}/{id}", new { done = true });
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(daily with { Done = true }, await response.Content.ReadFromJsonAsync<TaskView>());
                using var scope = factory.Services.CreateScope();
                var stored = await scope.ServiceProvider.GetRequiredService<HomeDbContext>().HomeTasks
                    .AsNoTracking().SingleAsync(task => task.Id == id);
                Assert.True(stored.RepeatsDaily);
                Assert.Equal(clock.UtcNow, stored.CompletedAtUtc);
            }

            clock.UtcNow = DateTimeOffset.Parse(after);
            using var restarted = new ApiFactory(database, clock, timeZone);
            using var restartedClient = restarted.CreateClient();
            var list = (await restartedClient.GetFromJsonAsync<TasksResponse>(BaseUrl))!.Items;
            Assert.False(list.Single(task => task.Id == id).Done);
            Assert.True(list.Single(task => task.Id == persistentId).Done);
            using (var scope = restarted.Services.CreateScope())
            {
                var stored = await scope.ServiceProvider.GetRequiredService<HomeDbContext>().HomeTasks
                    .AsNoTracking().SingleAsync(task => task.Id == id);
                Assert.Equal(DateTimeOffset.Parse(before), stored.CompletedAtUtc);
                Assert.True(stored.Done);
            }
            var updated = await restartedClient.PatchAsJsonAsync($"{BaseUrl}/{id}", new { title = "Editada" });
            Assert.False((await updated.Content.ReadFromJsonAsync<TaskView>())!.Done);
            updated = await restartedClient.PatchAsJsonAsync($"{BaseUrl}/{id}", new { done = true });
            Assert.True((await updated.Content.ReadFromJsonAsync<TaskView>())!.Done);
            using (var scope = restarted.Services.CreateScope())
            {
                var stored = await scope.ServiceProvider.GetRequiredService<HomeDbContext>().HomeTasks
                    .AsNoTracking().SingleAsync(task => task.Id == id);
                Assert.Equal(clock.UtcNow, stored.CompletedAtUtc);
            }
            updated = await restartedClient.PatchAsJsonAsync($"{BaseUrl}/{id}", new { done = false });
            Assert.False((await updated.Content.ReadFromJsonAsync<TaskView>())!.Done);
            using (var scope = restarted.Services.CreateScope())
            {
                var stored = await scope.ServiceProvider.GetRequiredService<HomeDbContext>().HomeTasks
                    .AsNoTracking().SingleAsync(task => task.Id == id);
                Assert.Null(stored.CompletedAtUtc);
            }
            Assert.Equal(HttpStatusCode.NoContent, (await restartedClient.DeleteAsync($"{BaseUrl}/{id}")).StatusCode);
        }
        finally
        {
            if (File.Exists(database)) File.Delete(database);
        }
    }

    [Fact]
    public async Task Http_Toggling_Recurrence_Preserves_State_And_Explicit_Done_Wins()
    {
        var clock = new TestTimeProvider(DateTimeOffset.Parse("2026-10-20T12:00:00Z"));
        using var factory = new ApiFactory(clock: clock);
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync(BaseUrl, new { title = "Afazer" });
        var id = (await created.Content.ReadFromJsonAsync<TaskView>())!.Id;
        await client.PatchAsJsonAsync($"{BaseUrl}/{id}", new { done = true });
        clock.UtcNow = clock.UtcNow.AddDays(1);
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{id}", new { repeatsDaily = true });
        var view = (await response.Content.ReadFromJsonAsync<TaskView>())!;
        Assert.True(view.Done);
        Assert.True(view.RepeatsDaily);
        clock.UtcNow = clock.UtcNow.AddDays(1);
        response = await client.PatchAsJsonAsync($"{BaseUrl}/{id}", new { repeatsDaily = false });
        view = (await response.Content.ReadFromJsonAsync<TaskView>())!;
        Assert.False(view.Done);
        Assert.False(view.RepeatsDaily);
        response = await client.PatchAsJsonAsync($"{BaseUrl}/{id}", new { repeatsDaily = true, done = true });
        view = (await response.Content.ReadFromJsonAsync<TaskView>())!;
        Assert.True(view.Done);
        Assert.True(view.RepeatsDaily);
        response = await client.PatchAsJsonAsync($"{BaseUrl}/{id}", new { repeatsDaily = false, done = false });
        view = (await response.Content.ReadFromJsonAsync<TaskView>())!;
        Assert.False(view.Done);
        Assert.False(view.RepeatsDaily);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"true\"")]
    [InlineData("0")]
    [InlineData("{}")]
    public async Task Http_Post_Rejects_Invalid_Recurrence(string value)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var body = new StringContent($"{{\"title\":\"Afazer\",\"repeatsDaily\":{value}}}",
            Encoding.UTF8, "application/json");
        await AssertProblem(await client.PostAsync(BaseUrl, body), HttpStatusCode.BadRequest);
        Assert.Equal(8, (await client.GetFromJsonAsync<TasksResponse>(BaseUrl))!.Items.Count);
    }

    [Fact]
    public void Invalid_Configured_TimeZone_Fails_Startup()
    {
        using var factory = new ApiFactory(timeZone: "Invalid/TimeZone");
        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }

    [Fact]
    public async Task Recurrence_Migration_Backfills_Existing_Completions_And_Preserves_Seed_And_Edits()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<HomeDbContext>().UseSqlite(connection).Options;
        await using var db = new HomeDbContext(options);
        var migrator = db.GetService<IMigrator>();
        const string previous = "20261020170156_AddHomeTasks";
        await migrator.MigrateAsync(previous);
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE HomeTasks SET Title = 'Editado', Done = 1 WHERE SortOrder = 1;
            """);
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        await db.Database.MigrateAsync();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        var tasks = await db.HomeTasks.AsNoTracking().OrderBy(task => task.SortOrder).ToListAsync();
        Assert.Equal(8, tasks.Count);
        Assert.Equal("Editado", tasks[0].Title);
        Assert.Equal(Enumerable.Range(1, 8).Select(index => new Guid($"30000000-0000-4000-8000-{index:000000000000}")),
            tasks.Select(task => task.Id));
        Assert.Equal(Enumerable.Range(1, 8).Select(index => (long)index), tasks.Select(task => task.SortOrder));
        foreach (var task in tasks)
        {
            Assert.False(task.RepeatsDaily);
            Assert.Equal(task.SortOrder <= 2, task.Done);
            Assert.Equal(task.Done, task.IsDone(after.AddDays(1), TimeZoneInfo.Utc));
            if (task.Done)
            {
                Assert.NotNull(task.CompletedAtUtc);
                Assert.Equal(TimeSpan.Zero, task.CompletedAtUtc.Value.Offset);
                Assert.InRange(task.CompletedAtUtc.Value, before, after);
            }
            else Assert.Null(task.CompletedAtUtc);
        }
        await db.Database.MigrateAsync();
        Assert.Equal(tasks.Select(task => task.CompletedAtUtc),
            await db.HomeTasks.AsNoTracking().OrderBy(task => task.SortOrder).Select(task => task.CompletedAtUtc).ToListAsync());
        await migrator.MigrateAsync(previous);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM HomeTasks WHERE Done = 1";
        Assert.Equal(2L, await command.ExecuteScalarAsync());
        command.CommandText = "SELECT Title FROM HomeTasks WHERE SortOrder = 1";
        Assert.Equal("Editado", await command.ExecuteScalarAsync());
        await db.Database.MigrateAsync();
        Assert.Equal(8, await db.HomeTasks.CountAsync());
    }
}
