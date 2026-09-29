using Homevault.Application.Ports;
using Homevault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Homevault.Infrastructure.Persistence;

public sealed class ShoppingRepository(HomeDbContext context) : IShoppingRepository
{
    public async Task<IReadOnlyList<ShoppingCategory>> GetActiveCategoriesAsync(Guid homeId, CancellationToken token) =>
        await context.ShoppingCategories.AsNoTracking()
            .Where(category => category.HomeId == homeId && category.IsActive)
            .OrderBy(category => category.SortOrder).ThenBy(category => category.Name).ToListAsync(token);

    public Task<ShoppingCategory?> GetCategoryAsync(Guid homeId, Guid id, CancellationToken token) =>
        context.ShoppingCategories.FirstOrDefaultAsync(category => category.HomeId == homeId && category.Id == id, token);

    public async Task<IReadOnlyList<ShoppingItem>> GetItemsAsync(Guid homeId, CancellationToken token) =>
        await context.ShoppingItems.AsNoTracking().Include(item => item.Category)
            .Where(item => item.HomeId == homeId)
            .OrderBy(item => item.CreatedAtUtc).ThenBy(item => item.Id).ToListAsync(token);

    public Task<ShoppingItem?> GetItemAsync(Guid homeId, Guid id, CancellationToken token) =>
        context.ShoppingItems.Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.HomeId == homeId && item.Id == id, token);

    public async Task AddAsync(ShoppingItem item, CancellationToken token)
    {
        context.ShoppingItems.Add(item);
        await context.SaveChangesAsync(token);
    }

    public Task SaveAsync(CancellationToken token) => context.SaveChangesAsync(token);

    public async Task DeleteAsync(ShoppingItem item, CancellationToken token)
    {
        context.ShoppingItems.Remove(item);
        await context.SaveChangesAsync(token);
    }

    public async Task<(int DeletedCount, IReadOnlyList<ShoppingItem> Remaining)> ClearPurchasedAsync(Guid homeId, CancellationToken token)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var deleted = await context.ShoppingItems.Where(item => item.HomeId == homeId && item.Purchased)
            .ExecuteDeleteAsync(token);
        var remaining = await GetItemsAsync(homeId, token);
        await transaction.CommitAsync(token);
        return (deleted, remaining);
    }
}
