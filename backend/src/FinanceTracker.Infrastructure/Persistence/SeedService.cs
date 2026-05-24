using FinanceTracker.Application.Abstractions;
using FinanceTracker.Domain;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Infrastructure.Persistence;

public class SeedService : ISeedService
{
    private static readonly (string Name, string Icon, string Color, CategoryType Type)[] Defaults =
    {
        ("Salary",        "cash-outline",            "#10b981", CategoryType.Income),
        ("Freelance",     "briefcase-outline",       "#22c55e", CategoryType.Income),
        ("Investments",   "trending-up-outline",     "#14b8a6", CategoryType.Income),
        ("Refund",        "return-up-back-outline",  "#06b6d4", CategoryType.Income),
        ("Other Income",  "add-circle-outline",      "#3b82f6", CategoryType.Income),

        ("Food & Dining", "restaurant-outline",      "#f97316", CategoryType.Expense),
        ("Groceries",     "basket-outline",          "#eab308", CategoryType.Expense),
        ("Transport",     "car-outline",             "#0ea5e9", CategoryType.Expense),
        ("Shopping",      "bag-outline",             "#ec4899", CategoryType.Expense),
        ("Bills & Utilities","receipt-outline",      "#8b5cf6", CategoryType.Expense),
        ("Entertainment", "film-outline",            "#a855f7", CategoryType.Expense),
        ("Health",        "medkit-outline",          "#ef4444", CategoryType.Expense),
        ("Education",     "school-outline",          "#6366f1", CategoryType.Expense),
        ("Travel",        "airplane-outline",        "#06b6d4", CategoryType.Expense),
        ("Rent",          "home-outline",            "#dc2626", CategoryType.Expense),
        ("Subscriptions", "tv-outline",              "#f43f5e", CategoryType.Expense),
        ("Other Expense", "ellipsis-horizontal",     "#64748b", CategoryType.Expense),
    };

    private readonly AppDbContext _db;
    public SeedService(AppDbContext db) { _db = db; }

    public async Task SeedDefaultCategoriesAsync(Guid organisationId, CancellationToken ct = default)
    {
        foreach (var d in Defaults)
        {
            var cat = Category.Create(organisationId, d.Name, d.Icon, d.Color, d.Type, isSystem: true);
            _db.Categories.Add(cat);
        }
        await _db.SaveChangesAsync(ct);
    }
}
