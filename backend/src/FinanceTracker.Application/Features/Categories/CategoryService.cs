using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using FinanceTracker.Domain;
using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Features.Categories;

public interface ICategoryService
{
    Task<Result<IReadOnlyList<CategoryDto>>> ListAsync(CancellationToken ct = default);
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest req, CancellationToken ct = default);
    Task<Result<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest req, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}

public class CategoryService : ICategoryService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;

    public CategoryService(IAppDbContext db, ICurrentUser current)
    {
        _db = db;
        _current = current;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> ListAsync(CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<IReadOnlyList<CategoryDto>>.Unauthorized();
        var list = await _db.Categories
            .OrderBy(c => c.Type).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Icon, c.Color, c.Type.ToString(), c.IsSystem))
            .ToListAsync(ct);
        return Result<IReadOnlyList<CategoryDto>>.Success(list);
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest req, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated || _current.OrganisationId is null)
            return Result<CategoryDto>.Unauthorized();

        if (!Enum.TryParse<CategoryType>(req.Type, true, out var type))
            return Result<CategoryDto>.Validation("Type must be Income or Expense");
        if (string.IsNullOrWhiteSpace(req.Name))
            return Result<CategoryDto>.Validation("Name is required");

        var duplicate = await _db.Categories
            .AnyAsync(c => c.Name.ToLower() == req.Name.Trim().ToLower() && c.Type == type, ct);
        if (duplicate) return Result<CategoryDto>.Conflict("A category with this name already exists");

        try
        {
            var cat = Category.Create(_current.OrganisationId.Value, req.Name, req.Icon, req.Color, type, isSystem: false);
            _db.Categories.Add(cat);
            await _db.SaveChangesAsync(ct);
            return Result<CategoryDto>.Success(new CategoryDto(cat.Id, cat.Name, cat.Icon, cat.Color, cat.Type.ToString(), cat.IsSystem));
        }
        catch (Domain.Common.DomainException ex)
        {
            return Result<CategoryDto>.Validation(ex.Message);
        }
    }

    public async Task<Result<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest req, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<CategoryDto>.Unauthorized();
        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cat is null) return Result<CategoryDto>.NotFound();
        try
        {
            cat.Update(req.Name, req.Icon, req.Color);
            await _db.SaveChangesAsync(ct);
            return Result<CategoryDto>.Success(new CategoryDto(cat.Id, cat.Name, cat.Icon, cat.Color, cat.Type.ToString(), cat.IsSystem));
        }
        catch (Domain.Common.DomainException ex)
        {
            return Result<CategoryDto>.Validation(ex.Message);
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result.Unauthorized();
        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cat is null) return Result.NotFound();
        if (cat.IsSystem) return Result.Conflict("System categories cannot be deleted");

        var inUse = await _db.Transactions.AnyAsync(t => t.CategoryId == id, ct)
                    || await _db.Budgets.AnyAsync(b => b.CategoryId == id, ct);
        if (inUse) return Result.Conflict("Category is in use and cannot be deleted");

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
