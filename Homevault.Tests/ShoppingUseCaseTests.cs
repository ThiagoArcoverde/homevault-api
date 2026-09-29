using Homevault.Application.Ports;
using Homevault.Application.Shopping;
using Homevault.Domain.Entities;
using Xunit;

namespace Homevault.Tests;

public sealed class ShoppingUseCaseTests
{
    [Fact]
    public void Category_And_Item_Enforce_Domain_Rules()
    {
        var homeId = Guid.NewGuid();
        var category = new ShoppingCategory(Guid.NewGuid(), homeId, "  Hortifruti  ", 10);
        Assert.Equal("Hortifruti", category.Name);
        Assert.Throws<ArgumentException>(() => new ShoppingCategory(Guid.NewGuid(), homeId, new string('x', 41), 10));
        Assert.Throws<ArgumentException>(() => new ShoppingItem(homeId, "  ", 1, category));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShoppingItem(homeId, "Maçã", 1000, category));
        var item = new ShoppingItem(homeId, "  Maçã  ", 1, category);
        Assert.Equal("Maçã", item.Name);
        Assert.False(item.Purchased);
        Assert.Equal(item.CreatedAtUtc, item.UpdatedAtUtc);
        category.Deactivate();
        Assert.Equal("Hortifruti", ShoppingItemView.From(item).Category.Name);
        Assert.Throws<ArgumentException>(() => item.Update(null, null, category, null));
    }

    [Fact]
    public async Task Service_Filters_Clamps_Summarizes_And_Clears_Only_Purchased()
    {
        var homeId = Guid.NewGuid();
        var fruit = new ShoppingCategory(Guid.NewGuid(), homeId, "Hortifruti", 10);
        var bakery = new ShoppingCategory(Guid.NewGuid(), homeId, "Padaria", 20);
        var otherHome = new ShoppingCategory(Guid.NewGuid(), Guid.NewGuid(), "Privado", 30);
        var repository = new FakeShoppingRepository(fruit, bakery, otherHome);
        var shopping = new ShoppingList(repository, homeId);

        var first = await shopping.CreateAsync("Café", 2, fruit.Id, default);
        var second = await shopping.CreateAsync("Cafe", 1, bakery.Id, default);
        var third = await shopping.CreateAsync("CAFÉ", 1, fruit.Id, default);
        await Assert.ThrowsAsync<ArgumentException>(() => shopping.CreateAsync("Fora", 1, otherHome.Id, default));
        await Assert.ThrowsAsync<ArgumentException>(() => shopping.ListAsync(null, otherHome.Id, 1, 4, default));

        var filtered = await shopping.ListAsync("  café ", fruit.Id, 99, 1, default);
        Assert.Equal(2, filtered.TotalMatchingItems);
        Assert.Equal(2, filtered.TotalPages);
        Assert.Equal(2, filtered.Page);
        Assert.Contains(filtered.Items.Single().Id, new[] { first.Id, third.Id });
        Assert.Equal(3, filtered.Summary.TotalItems);
        Assert.Equal(3, filtered.Summary.PendingItems);
        Assert.Equal(1, (await shopping.ListAsync("Café", null, 1, 4, default)).TotalPages);
        Assert.DoesNotContain((await shopping.ListAsync("Café", null, 1, 4, default)).Items, item => item.Id == second.Id);
        Assert.Equal(1, (await shopping.ListAsync("missing", null, 4, 4, default)).Page);
        await Assert.ThrowsAsync<ArgumentException>(() => shopping.ListAsync(null, null, 0, 4, default));

        var updated = await shopping.UpdateAsync(first.Id, new ShoppingPatch(null, null, null, true), default);
        Assert.True(updated!.Purchased);
        Assert.True(updated.UpdatedAtUtc >= updated.CreatedAtUtc);
        var snapshot = await shopping.ExportAsync(default);
        Assert.Equal(snapshot.Items.OrderBy(item => item.CreatedAtUtc).ThenBy(item => item.Id).Select(item => item.Id),
            snapshot.Items.Select(item => item.Id));
        Assert.Equal(1, snapshot.Summary.PurchasedItems);
        var cleared = await shopping.ClearAsync(default);
        Assert.Equal(1, cleared.DeletedCount);
        Assert.Equal(new ShoppingSummary(2, 0, 2), cleared.Summary);
        Assert.Equal(0, (await shopping.ClearAsync(default)).DeletedCount);
    }

    private sealed class FakeShoppingRepository(params ShoppingCategory[] categories) : IShoppingRepository
    {
        private readonly List<ShoppingItem> items = [];

        public Task<IReadOnlyList<ShoppingCategory>> GetActiveCategoriesAsync(Guid homeId, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<ShoppingCategory>>(categories.Where(c => c.HomeId == homeId && c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToArray());

        public Task<ShoppingCategory?> GetCategoryAsync(Guid homeId, Guid id, CancellationToken token) =>
            Task.FromResult(categories.FirstOrDefault(c => c.HomeId == homeId && c.Id == id));

        public Task<IReadOnlyList<ShoppingItem>> GetItemsAsync(Guid homeId, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<ShoppingItem>>(items.Where(i => i.HomeId == homeId)
                .OrderBy(i => i.CreatedAtUtc).ThenBy(i => i.Id).ToArray());

        public Task<ShoppingItem?> GetItemAsync(Guid homeId, Guid id, CancellationToken token) =>
            Task.FromResult(items.FirstOrDefault(i => i.HomeId == homeId && i.Id == id));

        public Task AddAsync(ShoppingItem item, CancellationToken token)
        {
            items.Add(item);
            return Task.CompletedTask;
        }

        public Task SaveAsync(CancellationToken token) => Task.CompletedTask;

        public Task DeleteAsync(ShoppingItem item, CancellationToken token)
        {
            items.Remove(item);
            return Task.CompletedTask;
        }

        public async Task<(int DeletedCount, IReadOnlyList<ShoppingItem> Remaining)> ClearPurchasedAsync(Guid homeId, CancellationToken token)
        {
            var deleted = items.RemoveAll(item => item.HomeId == homeId && item.Purchased);
            return (deleted, await GetItemsAsync(homeId, token));
        }
    }
}
