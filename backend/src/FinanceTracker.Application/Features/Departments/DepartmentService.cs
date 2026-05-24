using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Features.Departments;

public record DepartmentDto(
    Guid Id,
    string Name,
    Guid? ManagerMemberId,
    string? ManagerName,
    int MemberCount);

public record CreateDepartmentRequest(string Name, Guid? ManagerMemberId);
public record UpdateDepartmentRequest(string Name, Guid? ManagerMemberId);

public interface IDepartmentService
{
    Task<Result<IReadOnlyList<DepartmentDto>>> ListAsync(CancellationToken ct = default);
    Task<Result<DepartmentDto>> CreateAsync(CreateDepartmentRequest req, CancellationToken ct = default);
    Task<Result<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest req, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}

public class DepartmentService : IDepartmentService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IPermissionService _perms;
    private readonly IAuditLogService _audit;

    public DepartmentService(IAppDbContext db, ICurrentUser current, IPermissionService perms, IAuditLogService audit)
    {
        _db = db;
        _current = current;
        _perms = perms;
        _audit = audit;
    }

    public async Task<Result<IReadOnlyList<DepartmentDto>>> ListAsync(CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<IReadOnlyList<DepartmentDto>>.Unauthorized();

        var items = await (
            from d in _db.Departments.AsNoTracking().Where(x => x.IsActive)
            join m in _db.OrganisationMembers.IgnoreQueryFilters() on d.ManagerMemberId equals m.Id into mg
            from m in mg.DefaultIfEmpty()
            join u in _db.Users.IgnoreQueryFilters() on m.UserId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            select new DepartmentDto(
                d.Id, d.Name, d.ManagerMemberId,
                u != null ? u.FullName : null,
                _db.OrganisationMembers.Count(om => om.DepartmentId == d.Id && om.IsActive))
        ).ToListAsync(ct);

        return Result<IReadOnlyList<DepartmentDto>>.Success(items);
    }

    public async Task<Result<DepartmentDto>> CreateAsync(CreateDepartmentRequest req, CancellationToken ct = default)
    {
        var guard = AuthGuard<DepartmentDto>();
        if (guard is not null) return guard;

        if (await _db.Departments.AnyAsync(d => d.Name == req.Name.Trim() && d.IsActive, ct))
            return Result<DepartmentDto>.Conflict("Department with that name already exists");

        try
        {
            var dept = Department.Create(_current.OrganisationId!.Value, req.Name, req.ManagerMemberId);
            _db.Departments.Add(dept);
            await _db.SaveChangesAsync(ct);
            await _audit.LogAsync("department.created", "Department", dept.Id, null, new { dept.Name, dept.ManagerMemberId }, ct);
            return await Project(dept.Id, ct);
        }
        catch (DomainException ex)
        {
            return Result<DepartmentDto>.Validation(ex.Message);
        }
    }

    public async Task<Result<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest req, CancellationToken ct = default)
    {
        var guard = AuthGuard<DepartmentDto>();
        if (guard is not null) return guard;

        var dept = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (dept is null) return Result<DepartmentDto>.NotFound();

        try
        {
            var old = new { dept.Name, dept.ManagerMemberId };
            if (!string.IsNullOrWhiteSpace(req.Name)) dept.Rename(req.Name);
            dept.AssignManager(req.ManagerMemberId);
            await _db.SaveChangesAsync(ct);
            await _audit.LogAsync("department.updated", "Department", dept.Id, old, new { dept.Name, dept.ManagerMemberId }, ct);
            return await Project(dept.Id, ct);
        }
        catch (DomainException ex)
        {
            return Result<DepartmentDto>.Validation(ex.Message);
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.DepartmentManage))
            return Result.Forbidden();

        var dept = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (dept is null) return Result.NotFound();

        // Detach any members still pointing at this department.
        var members = await _db.OrganisationMembers.Where(m => m.DepartmentId == id).ToListAsync(ct);
        foreach (var m in members) m.AssignDepartment(null);
        dept.Deactivate();
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("department.deleted", "Department", id, new { dept.Name }, null, ct);
        return Result.Success();
    }

    private Result<T>? AuthGuard<T>()
    {
        if (!_current.IsAuthenticated || _current.OrganisationId is null) return Result<T>.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.DepartmentManage))
            return Result<T>.Forbidden();
        return null;
    }

    private async Task<Result<DepartmentDto>> Project(Guid id, CancellationToken ct)
    {
        var list = await ListAsync(ct);
        if (!list.IsSuccess) return Result<DepartmentDto>.Failure(list.Error ?? "Project failed");
        var dto = list.Value!.FirstOrDefault(d => d.Id == id);
        return dto is null ? Result<DepartmentDto>.NotFound() : Result<DepartmentDto>.Success(dto);
    }
}
