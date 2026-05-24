using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Icon { get; private set; } = default!;
    public string Color { get; private set; } = default!;
    public CategoryType Type { get; private set; }
    public bool IsSystem { get; private set; }

    private Category() { }

    public static Category Create(Guid organisationId, string name, string icon, string color, CategoryType type, bool isSystem = false)
    {
        if (organisationId == Guid.Empty) throw new DomainException("OrganisationId required");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name required");
        return new Category
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            Name = name.Trim(),
            Icon = string.IsNullOrWhiteSpace(icon) ? "ellipsis-horizontal" : icon.Trim(),
            Color = string.IsNullOrWhiteSpace(color) ? "#64748b" : color.Trim(),
            Type = type,
            IsSystem = isSystem
        };
    }

    public void Update(string name, string icon, string color)
    {
        if (IsSystem) throw new DomainException("System categories cannot be edited");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name required");
        Name = name.Trim();
        Icon = string.IsNullOrWhiteSpace(icon) ? Icon : icon.Trim();
        Color = string.IsNullOrWhiteSpace(color) ? Color : color.Trim();
    }
}
