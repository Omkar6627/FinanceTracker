namespace FinanceTracker.Application.Features.Categories;

public record CategoryDto(Guid Id, string Name, string Icon, string Color, string Type, bool IsSystem);
public record CreateCategoryRequest(string Name, string Icon, string Color, string Type);
public record UpdateCategoryRequest(string Name, string Icon, string Color);
