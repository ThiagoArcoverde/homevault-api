using System.Net;
using System.Net.Http.Json;
using Homevault.Application.Shopping;
using Homevault.Domain.Entities;
using Homevault.Infrastructure.Persistence;
using Homevault_api.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Homevault.Tests;

public sealed class ShoppingListTests
{
    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string database = Path.Combine(Path.GetTempPath(), $"homevault-test-{Guid.NewGuid()}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Homevault"] = $"Data Source={database};Pooling=False"
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveHostedWeather();
                services.RemoveAll<HomeDbContext>();
                services.RemoveAll<DbContextOptions<HomeDbContext>>();
                services.AddDbContext<HomeDbContext>(options => options.UseSqlite($"Data Source={database};Pooling=False"));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && File.Exists(database)) File.Delete(database);
        }
    }

    [Fact]
    public async Task Seed_Create_Validation_And_InactiveCategory()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var categories = await client.GetFromJsonAsync<ShoppingCategoriesResponse>("/api/v1/shopping-list/categories");
        Assert.NotNull(categories);
        Assert.Equal(9, categories.Categories.Count);
        Assert.Equal("Hortifruti", categories.Categories[0].Name);
        Assert.Equal(10, categories.Categories[0].SortOrder);

        var category = categories.Categories[0];
        var created = await client.PostAsJsonAsync("/api/v1/shopping-list/items",
            new { name = "  Maçã  ", quantity = 2, categoryId = category.Id });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var item = await created.Content.ReadFromJsonAsync<ShoppingItemView>();
        Assert.NotNull(item);
        Assert.Equal("Maçã", item.Name);
        Assert.False(item.Purchased);
        Assert.Equal(item.CreatedAtUtc, item.UpdatedAtUtc);
        Assert.Equal($"/api/v1/shopping-list/items/{item.Id}", created.Headers.Location?.ToString());
        var duplicate = await client.PostAsJsonAsync("/api/v1/shopping-list/items",
            new { name = "Maçã", quantity = 1, categoryId = category.Id });
        Assert.Equal(HttpStatusCode.Created, duplicate.StatusCode);
        Assert.NotEqual(item.Id, (await duplicate.Content.ReadFromJsonAsync<ShoppingItemView>())!.Id);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HomeDbContext>();
            var stored = await db.ShoppingCategories.SingleAsync(c => c.Id == category.Id);
            stored.Deactivate();
            await db.SaveChangesAsync();
        }
        var current = await client.GetFromJsonAsync<ShoppingItemView>($"/api/v1/shopping-list/items/{item.Id}");
        Assert.Equal("Hortifruti", current!.Category.Name);
        Assert.DoesNotContain((await client.GetFromJsonAsync<ShoppingCategoriesResponse>("/api/v1/shopping-list/categories"))!.Categories,
            c => c.Id == category.Id);
        var rejected = await client.PostAsJsonAsync("/api/v1/shopping-list/items", new { name = "Outra", quantity = 1, categoryId = category.Id });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/shopping-list/items",
            new { name = " ", quantity = 0, categoryId = Guid.Empty })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/shopping-list/items",
            new { name = "Válido", quantity = 1, categoryId = Guid.NewGuid() })).StatusCode);
    }

    [Fact]
    public async Task Filters_Pages_Export_And_Clear_Keep_GlobalSummary()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var categories = (await client.GetFromJsonAsync<ShoppingCategoriesResponse>("/api/v1/shopping-list/categories"))!.Categories;
        var ids = new List<Guid>();
        foreach (var (name, category) in new[] { ("Café", categories[0]), ("Cafe", categories[1]), ("café", categories[0]), ("Água", categories[1]), ("Leite", categories[1]) })
        {
            var response = await client.PostAsJsonAsync("/api/v1/shopping-list/items", new { name, quantity = 1, categoryId = category.Id });
            ids.Add((await response.Content.ReadFromJsonAsync<ShoppingItemView>())!.Id);
        }
        var updated = await client.PatchAsJsonAsync($"/api/v1/shopping-list/items/{ids[1]}", new { purchased = true });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.True((await updated.Content.ReadFromJsonAsync<ShoppingItemView>())!.Purchased);
        var page = await client.GetFromJsonAsync<ShoppingPage>("/api/v1/shopping-list/items?search=caf%C3%A9&page=99&pageSize=1");
        Assert.NotNull(page);
        Assert.Equal(2, page.TotalMatchingItems);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(2, page.Page);
        Assert.Equal(5, page.Summary.TotalItems);
        Assert.Equal(1, page.Summary.PurchasedItems);
        Assert.Single(page.Items);
        Assert.Equal(1, (await client.GetFromJsonAsync<ShoppingPage>($"/api/v1/shopping-list/items?categoryId={categories[0].Id}&pageSize=4"))!.TotalPages);
        var empty = await client.GetFromJsonAsync<ShoppingPage>("/api/v1/shopping-list/items?search=inexistente&page=100");
        Assert.Equal(1, empty!.Page);
        Assert.Equal(1, empty.TotalPages);
        Assert.Empty(empty.Items);
        var snapshot = await client.GetFromJsonAsync<ShoppingSnapshot>("/api/v1/shopping-list/export-data");
        Assert.Equal(5, snapshot!.Items.Count);
        Assert.Equal(5, snapshot.Summary.TotalItems);
        Assert.True(snapshot.GeneratedAtUtc.Offset == TimeSpan.Zero);
        Assert.Equal(snapshot.Items.OrderBy(item => item.CreatedAtUtc).ThenBy(item => item.Id).Select(item => item.Id),
            snapshot.Items.Select(item => item.Id));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/shopping-list/items?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/shopping-list/items?pageSize=101")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/shopping-list/items?categoryId=invalid")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/shopping-list/items?categoryId=" + Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync($"/api/v1/shopping-list/items/{ids[0]}", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync($"/api/v1/shopping-list/items/{ids[0]}", new { name = (string?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync($"/api/v1/shopping-list/items/{ids[0]}", new { unexpected = true })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync($"/api/v1/shopping-list/items/{ids[0]}", new { quantity = 1000 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/shopping-list/items/not-a-guid")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync("/api/v1/shopping-list/items/not-a-guid")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync("/api/v1/shopping-list/items?purchased=yes")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync("/api/v1/shopping-list/items?purchased=false")).StatusCode);
        var cleared = await client.DeleteAsync("/api/v1/shopping-list/items?purchased=true");
        Assert.Equal(1, (await cleared.Content.ReadFromJsonAsync<ClearPurchasedResult>())!.DeletedCount);
        Assert.Equal(0, (await (await client.DeleteAsync("/api/v1/shopping-list/items?purchased=true")).Content.ReadFromJsonAsync<ClearPurchasedResult>())!.DeletedCount);
        Assert.Equal(4, (await client.GetFromJsonAsync<ShoppingSnapshot>("/api/v1/shopping-list/export-data"))!.Items.Count);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/shopping-list/items/{ids[0]}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/v1/shopping-list/items/{ids[0]}")).StatusCode);
        var clamped = await client.GetFromJsonAsync<ShoppingPage>("/api/v1/shopping-list/items?page=2&pageSize=4");
        Assert.Equal(1, clamped!.Page);
        Assert.Equal(1, clamped.TotalPages);
        Assert.Equal(3, clamped.Summary.TotalItems);
    }

    [Fact]
    public async Task OtherHome_Is_Not_Visible_Or_Mutable()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        Guid otherItemId;
        Guid otherCategoryId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HomeDbContext>();
            var home = new Home("Outra casa");
            db.Homes.Add(home);
            var catalog = await db.ShoppingCategories.FirstAsync();
            var categoryId = Guid.NewGuid();
            otherCategoryId = categoryId;
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ShoppingCategories (Id, HomeId, Name, SortOrder, IsActive) VALUES ({categoryId}, {home.Id}, {"Outra categoria"}, {1}, {true})");
            var otherCategory = await db.ShoppingCategories.SingleAsync(c => c.Id == categoryId);
            var item = new ShoppingItem(home.Id, "Privado", 1, otherCategory);
            db.ShoppingItems.Add(item);
            await db.SaveChangesAsync();
            otherItemId = item.Id;
            Assert.NotEqual(catalog.HomeId, home.Id);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/shopping-list/items/{otherItemId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync($"/api/v1/shopping-list/items/{otherItemId}", new { purchased = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/v1/shopping-list/items/{otherItemId}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/shopping-list/items?categoryId={otherCategoryId}")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<ShoppingSnapshot>("/api/v1/shopping-list/export-data"))!.Items);
        await client.DeleteAsync("/api/v1/shopping-list/items?purchased=true");
        using var verify = factory.Services.CreateScope();
        Assert.True(await verify.ServiceProvider.GetRequiredService<HomeDbContext>().ShoppingItems.AnyAsync(i => i.Id == otherItemId));
    }
}

internal static class TestServices
{
    public static void RemoveHostedWeather(this IServiceCollection services)
    {
        var weather = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IHostedService) &&
            descriptor.ImplementationType?.Name == "WeatherCollectorBackgroundService");
        if (weather is not null) services.Remove(weather);
    }
}
