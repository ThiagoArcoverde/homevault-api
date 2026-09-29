namespace Homevault.Domain.Entities;

public class ShoppingCategory
{
    private ShoppingCategory() { }

    public ShoppingCategory(Guid id, Guid homeId, string name, int sortOrder)
    {
        if (id == Guid.Empty || homeId == Guid.Empty) throw new ArgumentException("IDs de categoria e casa são obrigatórios.");
        name = name?.Trim() ?? string.Empty;
        if (name.Length is < 1 or > 40) throw new ArgumentException("O nome deve ter entre 1 e 40 caracteres.", nameof(name));
        Id = id;
        HomeId = homeId;
        Name = name;
        SortOrder = sortOrder;
        IsActive = true;
    }

    public void Deactivate() => IsActive = false;

    public Guid Id { get; private set; }
    public Guid HomeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public Home Home { get; private set; } = null!;
}
