using Homevault.Domain.Entities;

namespace Homevault.Application.Ports;

public interface IShoppingRepository
{
    Task<IReadOnlyList<ShoppingCategory>> GetActiveCategoriesAsync(Guid homeId, CancellationToken cancellationToken);
    Task<ShoppingCategory?> GetCategoryAsync(Guid homeId, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ShoppingItem>> GetItemsAsync(Guid homeId, CancellationToken cancellationToken);
    Task<ShoppingItem?> GetItemAsync(Guid homeId, Guid id, CancellationToken cancellationToken);
    Task AddAsync(ShoppingItem item, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    Task DeleteAsync(ShoppingItem item, CancellationToken cancellationToken);
    Task<(int DeletedCount, IReadOnlyList<ShoppingItem> Remaining)> ClearPurchasedAsync(Guid homeId, CancellationToken cancellationToken);
}
