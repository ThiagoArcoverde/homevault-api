using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Homevault.Application.Tasks;
using Homevault.Domain.Entities;
using Homevault.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Homevault.Tests;

public sealed class TasksTests
{
    private const string BaseUrl = "/api/v1/tasks";
    private const string Origin = "http://localhost:5173";

    private sealed class ApiFactory(string? existingDatabase = null) : WebApplicationFactory<Program>
    {
        private readonly string database = existingDatabase ??
            Path.Combine(Path.GetTempPath(), $"homevault-tasks-test-{Guid.NewGuid()}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Homevault"] = $"Data Source={database};Pooling=False",
                    ["Cors:FrontendOrigins:0"] = Origin
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveHostedWeather();
                services.RemoveAll<HomeDbContext>();
                services.RemoveAll<DbContextOptions<HomeDbContext>>();
                services.AddDbContext<HomeDbContext>(options =>
                    options.UseSqlite($"Data Source={database};Pooling=False"));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && existingDatabase is null && File.Exists(database)) File.Delete(database);
        }
    }

    [Fact]
    public async Task Seed_Has_Exact_Values_Order_Ids_And_Json_Shape()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync(BaseUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "items" }, json.EnumerateObject().Select(property => property.Name));
        var items = json.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(8, items.Length);
        var expected = new[]
        {
            ("Levar o lixo para fora", "Hoje, até 20:00", "Alex", false),
            ("Regar as plantas da varanda", "Hoje, até 18:00", "Rafa", true),
            ("Conferir a lista de compras", "Sem prazo", "Todos", false),
            ("Limpar o filtro do ar-condicionado", "Amanhã", "Casa", false),
            ("Organizar a bancada da cozinha", "Amanhã", "Alex", false),
            ("Conferir a caixa de correio", "Sem prazo", "Rafa", false),
            ("Recolher as roupas do varal", "Sábado", "Todos", false),
            ("Verificar o filtro de água", "Segunda-feira", "Casa", false)
        };
        for (var index = 0; index < items.Length; index++)
        {
            Assert.Equal(new[] { "assignee", "done", "id", "meta", "title" },
                items[index].EnumerateObject().Select(property => property.Name).Order());
            Assert.Equal(new Guid($"30000000-0000-4000-8000-{index + 1:000000000000}"), items[index].GetProperty("id").GetGuid());
            Assert.Equal(expected[index].Item1, items[index].GetProperty("title").GetString());
            Assert.Equal(expected[index].Item2, items[index].GetProperty("meta").GetString());
            Assert.Equal(expected[index].Item3, items[index].GetProperty("assignee").GetString());
            Assert.Equal(expected[index].Item4, items[index].GetProperty("done").GetBoolean());
        }
        using var scope = factory.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<HomeDbContext>().HomeTasks
            .OrderBy(task => task.SortOrder).ToListAsync();
        Assert.Equal(Enumerable.Range(1, 8).Select(value => (long)value), stored.Select(task => task.SortOrder));
    }

    [Fact]
    public async Task Crud_Preserves_Order_Defaults_And_Partial_Updates_Across_Restart()
    {
        var database = Path.Combine(Path.GetTempPath(), $"homevault-tasks-restart-{Guid.NewGuid()}.db");
        try
        {
            Guid id;
            Guid deletedId;
            Guid seedId;
            using (var factory = new ApiFactory(database))
            using (var client = factory.CreateClient())
            {
                seedId = (await client.GetFromJsonAsync<TasksResponse>(BaseUrl))!.Items[0].Id;
                var response = await client.PostAsJsonAsync(BaseUrl, new { title = "  Nova tarefa  " });
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                var created = (await response.Content.ReadFromJsonAsync<TaskView>())!;
                id = created.Id;
                Assert.NotEqual(Guid.Empty, id);
                Assert.Equal("Nova tarefa", created.Title);
                Assert.Equal("Sem prazo", created.Meta);
                Assert.Equal("Todos", created.Assignee);
                Assert.False(created.Done);
                Assert.Equal($"{BaseUrl}/{id}", response.Headers.Location?.ToString());
                var second = await client.PostAsJsonAsync(BaseUrl, new { title = "Nova tarefa", meta = " Hoje ", assignee = " Alex " });
                Assert.Equal(HttpStatusCode.Created, second.StatusCode);
                var duplicate = (await second.Content.ReadFromJsonAsync<TaskView>())!;
                deletedId = duplicate.Id;
                Assert.NotEqual(id, deletedId);
                Assert.Equal("Hoje", duplicate.Meta);
                Assert.Equal("Alex", duplicate.Assignee);

                var updated = await client.PatchAsJsonAsync($"{BaseUrl}/{id}", new { done = true });
                Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
                Assert.Equal(created with { Done = true }, await updated.Content.ReadFromJsonAsync<TaskView>());
                updated = await client.PatchAsJsonAsync($"{BaseUrl}/{id}",
                    new { title = "  Editada  ", meta = "  Amanhã  ", assignee = "  Rafa  ", done = false });
                Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
                Assert.Equal(new TaskView(id, "Editada", "Amanhã", "Rafa", false),
                    await updated.Content.ReadFromJsonAsync<TaskView>());
                var list = (await client.GetFromJsonAsync<TasksResponse>(BaseUrl))!.Items;
                Assert.Equal(new[] { id, deletedId }, list.TakeLast(2).Select(task => task.Id));
                Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{BaseUrl}/{deletedId}")).StatusCode);
                Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{BaseUrl}/{seedId}")).StatusCode);
            }

            using var restarted = new ApiFactory(database);
            using var restartedClient = restarted.CreateClient();
            var persisted = (await restartedClient.GetFromJsonAsync<TasksResponse>(BaseUrl))!.Items;
            Assert.Equal(8, persisted.Count);
            Assert.Equal(new TaskView(id, "Editada", "Amanhã", "Rafa", false), persisted[^1]);
            Assert.DoesNotContain(persisted, task => task.Id == seedId || task.Id == deletedId);
        }
        finally
        {
            if (File.Exists(database)) File.Delete(database);
        }
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"title\":null}")]
    [InlineData("{\"title\":\" \"}")]
    [InlineData("{\"meta\":null}")]
    [InlineData("{\"assignee\":null}")]
    [InlineData("{\"done\":null}")]
    [InlineData("{\"done\":\"true\"}")]
    [InlineData("{\"unknown\":true}")]
    [InlineData("{\"done\":true,\"unknown\":1}")]
    [InlineData("{\"hasTitle\":true}")]
    [InlineData("{\"done\":true,\"hasTitle\":true}")]
    [InlineData("{\"done\":true,\"hasAnyField\":true}")]
    [InlineData("{\"done\":true,\"sortOrder\":1}")]
    [InlineData("{\"done\":true,\"id\":\"30000000-0000-4000-8000-000000000001\"}")]
    public async Task Invalid_Patch_Returns_Problem_And_Does_Not_Mutate(string body)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var before = (await client.GetFromJsonAsync<TasksResponse>(BaseUrl))!.Items;
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        await AssertProblem(await client.PatchAsync($"{BaseUrl}/{before[0].Id}", content), HttpStatusCode.BadRequest);
        Assert.Equal(before, (await client.GetFromJsonAsync<TasksResponse>(BaseUrl))!.Items);
    }

    [Fact]
    public async Task Invalid_Create_Lengths_Ids_And_Missing_Tasks_Return_Problems()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        foreach (var body in new object[]
        {
            new { }, new { title = (string?)null }, new { title = " " }, new { title = new string('t', 121) },
            new { title = "Titulo", meta = " " }, new { title = "Titulo", meta = new string('m', 61) },
            new { title = "Titulo", assignee = " " }, new { title = "Titulo", assignee = new string('a', 41) }
        })
            await AssertProblem(await client.PostAsJsonAsync(BaseUrl, body), HttpStatusCode.BadRequest);

        var id = (await client.GetFromJsonAsync<TasksResponse>(BaseUrl))!.Items[0].Id;
        foreach (var body in new object[]
        {
            new { title = new string('t', 121) }, new { meta = new string('m', 61) },
            new { assignee = new string('a', 41) }
        })
            await AssertProblem(await client.PatchAsJsonAsync($"{BaseUrl}/{id}", body), HttpStatusCode.BadRequest);

        await AssertProblem(await client.PatchAsJsonAsync($"{BaseUrl}/invalid", new { done = true }), HttpStatusCode.BadRequest);
        await AssertProblem(await client.DeleteAsync($"{BaseUrl}/invalid"), HttpStatusCode.BadRequest);
        await AssertProblem(await client.PatchAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}", new { done = true }), HttpStatusCode.NotFound);
        await AssertProblem(await client.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{BaseUrl}/{id}")).StatusCode);
        await AssertProblem(await client.DeleteAsync($"{BaseUrl}/{id}"), HttpStatusCode.NotFound);

        var accepted = await client.PostAsJsonAsync(BaseUrl,
            new { title = $" {new string('t', 120)} ", meta = $" {new string('m', 60)} ", assignee = $" {new string('a', 40)} " });
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
    }

    [Fact]
    public async Task Other_Home_Is_Not_Visible_Or_Mutable()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        Guid id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HomeDbContext>();
            var home = new Home("Outra casa");
            db.Homes.Add(home);
            await db.SaveChangesAsync();
            var task = new TodoTask(home.Id, "Privado");
            await new TaskRepository(db).AddAsync(task, default);
            id = task.Id;
        }
        Assert.DoesNotContain((await client.GetFromJsonAsync<TasksResponse>(BaseUrl))!.Items, task => task.Id == id);
        await AssertProblem(await client.PatchAsJsonAsync($"{BaseUrl}/{id}", new { done = true }), HttpStatusCode.NotFound);
        await AssertProblem(await client.DeleteAsync($"{BaseUrl}/{id}"), HttpStatusCode.NotFound);
        using var verify = factory.Services.CreateScope();
        Assert.False((await verify.ServiceProvider.GetRequiredService<HomeDbContext>().HomeTasks.SingleAsync(task => task.Id == id)).Done);
    }

    [Fact]
    public async Task Concurrent_Creates_Receive_Unique_Stable_Positions()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var responses = await Task.WhenAll(Enumerable.Range(1, 12)
            .Select(index => client.PostAsJsonAsync(BaseUrl, new { title = $"Tarefa {index}" })));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        using var scope = factory.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<HomeDbContext>().HomeTasks
            .OrderBy(task => task.SortOrder).ToListAsync();
        Assert.Equal(Enumerable.Range(1, 20).Select(value => (long)value), stored.Select(task => task.SortOrder));
        var first = (await client.GetFromJsonAsync<TasksResponse>(BaseUrl))!.Items;
        Assert.Equal(stored.Select(task => task.Id), first.Select(task => task.Id));
        Assert.Equal(first, (await client.GetFromJsonAsync<TasksResponse>(BaseUrl))!.Items);
    }

    [Fact]
    public async Task Swagger_And_Cors_Expose_Contract()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var swagger = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var paths = swagger.GetProperty("paths");
        Assert.True(paths.GetProperty(BaseUrl).TryGetProperty("get", out _));
        Assert.True(paths.GetProperty(BaseUrl).TryGetProperty("post", out _));
        Assert.True(paths.GetProperty($"{BaseUrl}/{{id}}").TryGetProperty("patch", out _));
        Assert.True(paths.GetProperty($"{BaseUrl}/{{id}}").TryGetProperty("delete", out _));
        foreach (var method in new[] { "PATCH", "DELETE" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, $"{BaseUrl}/{Guid.NewGuid()}");
            request.Headers.Add("Origin", Origin);
            request.Headers.Add("Access-Control-Request-Method", method);
            request.Headers.Add("Access-Control-Request-Headers", "content-type");
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(Origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
            Assert.Contains(method, string.Join(",", response.Headers.GetValues("Access-Control-Allow-Methods")));
        }
    }

    [Fact]
    public async Task Migration_Upgrades_Existing_Database_And_Rolls_Back_Without_Changing_Shopping()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<HomeDbContext>().UseSqlite(connection).Options;
        await using var db = new HomeDbContext(options);
        var migrator = db.GetService<IMigrator>();
        const string previous = "20260930175521_CapitalizePetShoppingCategory";
        await migrator.MigrateAsync(previous);
        var category = await db.ShoppingCategories.FirstAsync();
        var shopping = new ShoppingItem(category.HomeId, "Preservado", 2, category);
        db.ShoppingItems.Add(shopping);
        await db.SaveChangesAsync();

        await db.Database.MigrateAsync();
        Assert.Equal(8, await db.HomeTasks.CountAsync());
        Assert.Equal("Preservado", (await db.ShoppingItems.AsNoTracking().SingleAsync()).Name);
        var task = await db.HomeTasks.OrderBy(item => item.SortOrder).FirstAsync();
        task.Update("Editado", null, null, true);
        await db.SaveChangesAsync();
        await db.Database.MigrateAsync();
        Assert.Equal("Editado", (await db.HomeTasks.AsNoTracking().SingleAsync(item => item.Id == task.Id)).Title);
        await migrator.MigrateAsync(previous);
        Assert.Single(await db.ShoppingItems.AsNoTracking().ToListAsync());
        Assert.DoesNotContain(await db.Database.GetAppliedMigrationsAsync(), migration => migration.EndsWith("_AddHomeTasks"));
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)status, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
    }
}
