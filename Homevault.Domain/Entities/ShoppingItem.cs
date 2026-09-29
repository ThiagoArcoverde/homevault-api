namespace Homevault.Domain.Entities;

public class ShoppingItem
{
    private ShoppingItem() { }

    public ShoppingItem(Guid homeId, string name, int quantity, ShoppingCategory category)
    {
        Id = Guid.NewGuid();
        HomeId = homeId;
        SetName(name);
        SetQuantity(quantity);
        SetCategory(category);
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid HomeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public Guid CategoryId { get; private set; }
    public ShoppingCategory Category { get; private set; } = null!;
    public Home Home { get; private set; } = null!;
    public bool Purchased { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(string? name, int? quantity, ShoppingCategory? category, bool? purchased)
    {
        if (name is not null) SetName(name);
        if (quantity.HasValue) SetQuantity(quantity.Value);
        if (category is not null) SetCategory(category);
        if (purchased.HasValue) Purchased = purchased.Value;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void SetName(string name)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length is < 1 or > 80) throw new ArgumentException("O nome deve ter entre 1 e 80 caracteres.", nameof(name));
        Name = name;
    }

    private void SetQuantity(int quantity)
    {
        if (quantity is < 1 or > 999) throw new ArgumentOutOfRangeException(nameof(quantity));
        Quantity = quantity;
    }

    private void SetCategory(ShoppingCategory category)
    {
        if (!category.IsActive || category.HomeId != HomeId)
            throw new ArgumentException("A categoria deve estar ativa na casa atual.", nameof(category));
        CategoryId = category.Id;
        Category = category;
    }
}
