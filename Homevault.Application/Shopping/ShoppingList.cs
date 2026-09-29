using Homevault.Application.Ports;
using Homevault.Domain.Entities;

namespace Homevault.Application.Shopping;

public sealed record ShoppingCategoryView(Guid Id, string Name, int SortOrder);
public sealed record ItemCategoryView(Guid Id, string Name);
public sealed record ShoppingItemView(Guid Id, string Name, int Quantity, ItemCategoryView Category,
    bool Purchased, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc)
{
    public static ShoppingItemView From(ShoppingItem item) => new(item.Id, item.Name, item.Quantity,
        new ItemCategoryView(item.CategoryId, item.Category.Name), item.Purchased, item.CreatedAtUtc, item.UpdatedAtUtc);
}
public sealed record ShoppingSummary(int TotalItems, int PurchasedItems, int PendingItems);
public sealed record ShoppingPage(IReadOnlyList<ShoppingItemView> Items, int Page, int PageSize,
    int TotalMatchingItems, int TotalPages, ShoppingSummary Summary);
public sealed record ShoppingSnapshot(IReadOnlyList<ShoppingItemView> Items, ShoppingSummary Summary, DateTimeOffset GeneratedAtUtc);
public sealed record ClearPurchasedResult(int DeletedCount, ShoppingSummary Summary);
public sealed record ShoppingPatch(string? Name, int? Quantity, Guid? CategoryId, bool? Purchased);

public sealed class ShoppingList(IShoppingRepository repository, Guid homeId)
{
    public async Task<IReadOnlyList<ShoppingCategoryView>> CategoriesAsync(CancellationToken token) =>
        (await repository.GetActiveCategoriesAsync(homeId, token))
        .Select(category => new ShoppingCategoryView(category.Id, category.Name, category.SortOrder)).ToArray();

    public async Task<ShoppingPage> ListAsync(string? search, Guid? categoryId, int page, int pageSize, CancellationToken token)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw new ArgumentException("Paginação inválida.");
        if (categoryId.HasValue && await repository.GetCategoryAsync(homeId, categoryId.Value, token) is null)
            throw new ArgumentException("Categoria desconhecida.");

        var all = await repository.GetItemsAsync(homeId, token);
        var matching = all.Where(item =>
            (categoryId is null || item.CategoryId == categoryId) &&
            (string.IsNullOrWhiteSpace(search) || item.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))).ToArray();
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)matching.Length / pageSize));
        var actualPage = Math.Min(page, totalPages);
        return new ShoppingPage(matching.Skip((actualPage - 1) * pageSize).Take(pageSize).Select(ShoppingItemView.From).ToArray(),
            actualPage, pageSize, matching.Length, totalPages, Summarize(all));
    }

    public async Task<ShoppingSnapshot> ExportAsync(CancellationToken token)
    {
        var all = await repository.GetItemsAsync(homeId, token);
        return new ShoppingSnapshot(all.Select(ShoppingItemView.From).ToArray(), Summarize(all), DateTimeOffset.UtcNow);
    }

    public async Task<ShoppingItemView?> FindAsync(Guid id, CancellationToken token)
    {
        var item = await repository.GetItemAsync(homeId, id, token);
        return item is null ? null : ShoppingItemView.From(item);
    }

    public async Task<ShoppingItemView> CreateAsync(string name, int quantity, Guid categoryId, CancellationToken token)
    {
        var category = await ActiveCategoryAsync(categoryId, token);
        var item = new ShoppingItem(homeId, name, quantity, category);
        await repository.AddAsync(item, token);
        return ShoppingItemView.From(item);
    }

    public async Task<ShoppingItemView?> UpdateAsync(Guid id, ShoppingPatch patch, CancellationToken token)
    {
        var item = await repository.GetItemAsync(homeId, id, token);
        if (item is null) return null;
        var category = patch.CategoryId.HasValue ? await ActiveCategoryAsync(patch.CategoryId.Value, token) : null;
        item.Update(patch.Name, patch.Quantity, category, patch.Purchased);
        await repository.SaveAsync(token);
        return ShoppingItemView.From(item);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken token)
    {
        var item = await repository.GetItemAsync(homeId, id, token);
        if (item is null) return false;
        await repository.DeleteAsync(item, token);
        return true;
    }

    public async Task<ClearPurchasedResult> ClearAsync(CancellationToken token)
    {
        var result = await repository.ClearPurchasedAsync(homeId, token);
        return new ClearPurchasedResult(result.DeletedCount, Summarize(result.Remaining));
    }

    private async Task<ShoppingCategory> ActiveCategoryAsync(Guid id, CancellationToken token)
    {
        var category = await repository.GetCategoryAsync(homeId, id, token);
        if (category is null || !category.IsActive) throw new ArgumentException("Categoria desconhecida ou inativa.");
        return category;
    }

    private static ShoppingSummary Summarize(IReadOnlyList<ShoppingItem> items)
    {
        var purchased = items.Count(item => item.Purchased);
        return new ShoppingSummary(items.Count, purchased, items.Count - purchased);
    }
}
