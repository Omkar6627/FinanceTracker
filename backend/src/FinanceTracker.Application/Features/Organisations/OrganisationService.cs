using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using FinanceTracker.Domain;
using FinanceTracker.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Features.Organisations;

public record OrganisationDto(
    Guid Id,
    string Name,
    string Mode,
    string Currency,
    int MemberCount,
    int DepartmentCount);

public record UpdateOrganisationRequest(string Name, string Currency);
public record SwitchModeRequest(string Mode);

public interface IOrganisationService
{
    Task<Result<OrganisationDto>> GetAsync(CancellationToken ct = default);
    Task<Result<OrganisationDto>> UpdateAsync(UpdateOrganisationRequest req, CancellationToken ct = default);
    Task<Result<OrganisationDto>> SwitchModeAsync(SwitchModeRequest req, CancellationToken ct = default);
}

public class OrganisationService : IOrganisationService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IPermissionService _perms;
    private readonly IAuditLogService _audit;

    public OrganisationService(IAppDbContext db, ICurrentUser current, IPermissionService perms, IAuditLogService audit)
    {
        _db = db;
        _current = current;
        _perms = perms;
        _audit = audit;
    }

    public async Task<Result<OrganisationDto>> GetAsync(CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated || _current.OrganisationId is null) return Result<OrganisationDto>.Unauthorized();
        return await Project(_current.OrganisationId.Value, ct);
    }

    public async Task<Result<OrganisationDto>> UpdateAsync(UpdateOrganisationRequest req, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated || _current.OrganisationId is null) return Result<OrganisationDto>.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.SettingsManage))
            return Result<OrganisationDto>.Forbidden();

        var org = await _db.Organisations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == _current.OrganisationId.Value, ct);
        if (org is null) return Result<OrganisationDto>.NotFound();

        try
        {
            if (!string.IsNullOrWhiteSpace(req.Name)) org.Rename(req.Name);
            if (!string.IsNullOrWhiteSpace(req.Currency)) org.ChangeCurrency(req.Currency);
            await _db.SaveChangesAsync(ct);
            return await Project(org.Id, ct);
        }
        catch (DomainException ex)
        {
            return Result<OrganisationDto>.Validation(ex.Message);
        }
    }

    public async Task<Result<OrganisationDto>> SwitchModeAsync(SwitchModeRequest req, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated || _current.OrganisationId is null) return Result<OrganisationDto>.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.OrganisationModeSwitch))
            return Result<OrganisationDto>.Forbidden();

        if (!Enum.TryParse<OrganisationMode>(req.Mode, true, out var mode))
            return Result<OrganisationDto>.Validation("Mode must be Individual or Enterprise");

        var org = await _db.Organisations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == _current.OrganisationId.Value, ct);
        if (org is null) return Result<OrganisationDto>.NotFound();

        // Going back to Individual requires the org to have no active members other than the Owner.
        if (mode == OrganisationMode.Individual && org.Mode == OrganisationMode.Enterprise)
        {
            var others = await _db.OrganisationMembers.IgnoreQueryFilters()
                .CountAsync(m => m.OrganisationId == org.Id && m.IsActive && m.Role != MemberRole.Owner, ct);
            if (others > 0)
                return Result<OrganisationDto>.Validation("Remove non-owner members before switching back to Individual mode");
        }

        var oldMode = org.Mode.ToString();
        org.SwitchMode(mode);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("organisation.mode_switched", "Organisation", org.Id,
            new { Mode = oldMode }, new { Mode = org.Mode.ToString() }, ct);

        return await Project(org.Id, ct);
    }

    private async Task<Result<OrganisationDto>> Project(Guid id, CancellationToken ct)
    {
        var org = await _db.Organisations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == id, ct);
        if (org is null) return Result<OrganisationDto>.NotFound();
        var memberCount = await _db.OrganisationMembers.IgnoreQueryFilters().CountAsync(m => m.OrganisationId == id && m.IsActive, ct);
        var deptCount = await _db.Departments.IgnoreQueryFilters().CountAsync(d => d.OrganisationId == id && d.IsActive, ct);
        return Result<OrganisationDto>.Success(new OrganisationDto(org.Id, org.Name, org.Mode.ToString(), org.Currency, memberCount, deptCount));
    }
}
