namespace Homevault.Domain.Entities;

public class TodoTask
{
    public const string DefaultMeta = "Sem prazo";
    public const string DefaultAssignee = "Todos";

    private TodoTask() { }

    public TodoTask(Guid homeId, string title, string? meta = null, string? assignee = null)
    {
        Id = Guid.NewGuid();
        HomeId = homeId;
        Title = ValidateText(title, 120, nameof(title));
        Meta = ValidateText(meta ?? DefaultMeta, 60, nameof(meta));
        Assignee = ValidateText(assignee ?? DefaultAssignee, 40, nameof(assignee));
    }

    public Guid Id { get; private set; }
    public Guid HomeId { get; private set; }
    public Home Home { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public string Meta { get; private set; } = string.Empty;
    public string Assignee { get; private set; } = string.Empty;
    public bool Done { get; private set; }
    public long SortOrder { get; private set; }

    public void Update(string? title, string? meta, string? assignee, bool? done)
    {
        var nextTitle = title is null ? Title : ValidateText(title, 120, nameof(title));
        var nextMeta = meta is null ? Meta : ValidateText(meta, 60, nameof(meta));
        var nextAssignee = assignee is null ? Assignee : ValidateText(assignee, 40, nameof(assignee));
        Title = nextTitle;
        Meta = nextMeta;
        Assignee = nextAssignee;
        if (done.HasValue) Done = done.Value;
    }

    private static string ValidateText(string? value, int maximum, string parameter)
    {
        value = value?.Trim() ?? string.Empty;
        if (value.Length < 1 || value.Length > maximum)
            throw new ArgumentException($"O texto deve ter entre 1 e {maximum} caracteres.", parameter);
        return value;
    }
}
