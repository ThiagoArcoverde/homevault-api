namespace Homevault.Domain.Entities;

public class Home
{
    private Home()
    {
    }

    public Home(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("O nome da casa é obrigatório.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }
}
