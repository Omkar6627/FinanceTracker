using System.Security.Cryptography;
using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using FinanceTracker.Domain;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Features.Members;

public record MemberDto(
    Guid Id,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    Guid? DepartmentId,
    string? DepartmentName,
    bool IsActive,
    DateTime JoinedAt);

public record InvitationDto(
    Guid Id,
    string Email,
    string Role,
    Guid? DepartmentId,
    string? DepartmentName,
    string Status,
    DateTimeOffset ExpiresAt,
    string Token);

public record InviteMemberRequest(string Email, string Role, Guid? DepartmentId);
public record AcceptInviteRequest(string Token, string FullName, string Password);
public record ChangeRoleRequest(string Role, Guid? DepartmentId);

public interface IMemberService
{
    Task<Result<IReadOnlyList<MemberDto>>> ListAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<InvitationDto>>> ListInvitationsAsync(CancellationToken ct = default);
    Task<Result<InvitationDto>> InviteAsync(InviteMemberRequest req, CancellationToken ct = default);
    Task<Result> RevokeInvitationAsync(Guid id, CancellationToken ct = default);
    Task<Result<MemberDto>> ChangeRoleAsync(Guid memberId, ChangeRoleRequest req, CancellationToken ct = default);
    Task<Result> RemoveAsync(Guid memberId, CancellationToken ct = default);
}

public class MemberService : IMemberService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IPermissionService _perms;
    private readonly IAuditLogService _audit;

    public MemberService(IAppDbContext db, ICurrentUser current, IPermissionService perms, IAuditLogService audit)
    {
        _db = db;
        _current = current;
        _perms = perms;
        _audit = audit;
    }

    public async Task<Result<IReadOnlyList<MemberDto>>> ListAsync(CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<IReadOnlyList<MemberDto>>.Unauthorized();

        var items = await (
            from m in _db.OrganisationMembers.AsNoTracking()
            join u in _db.Users.IgnoreQueryFilters() on m.UserId equals u.Id
            join d in _db.Departments.IgnoreQueryFilters() on m.DepartmentId equals d.Id into dg
            from d in dg.DefaultIfEmpty()
            select new MemberDto(
                m.Id, u.Id, u.Email, u.FullName, m.Role.ToString(),
                m.DepartmentId, d != null ? d.Name : null,
                m.IsActive, m.JoinedAt)
        ).ToListAsync(ct);

        return Result<IReadOnlyList<MemberDto>>.Success(items);
    }

    public async Task<Result<IReadOnlyList<InvitationDto>>> ListInvitationsAsync(CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<IReadOnlyList<InvitationDto>>.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.MemberInvite))
            return Result<IReadOnlyList<InvitationDto>>.Forbidden();

        var items = await (
            from i in _db.Invitations.AsNoTracking()
            join d in _db.Departments on i.DepartmentId equals d.Id into dg
            from d in dg.DefaultIfEmpty()
            orderby i.CreatedAt descending
            select new InvitationDto(
                i.Id, i.Email, i.Role.ToString(),
                i.DepartmentId, d != null ? d.Name : null,
                i.Status.ToString(), i.ExpiresAt, i.Token)
        ).ToListAsync(ct);

        return Result<IReadOnlyList<InvitationDto>>.Success(items);
    }

    public async Task<Result<InvitationDto>> InviteAsync(InviteMemberRequest req, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated || _current.OrganisationId is null || _current.UserId is null)
            return Result<InvitationDto>.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.MemberInvite))
            return Result<InvitationDto>.Forbidden();

        if (!Enum.TryParse<MemberRole>(req.Role, true, out var role) || role == MemberRole.Owner)
            return Result<InvitationDto>.Validation("Role must be Admin, Manager, Member, or Viewer");

        var email = req.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<InvitationDto>.Validation("Valid email required");

        var existing = await _db.Invitations.AnyAsync(i => i.Email == email && i.Status == InvitationStatus.Pending, ct);
        if (existing) return Result<InvitationDto>.Conflict("An invitation for that email is already pending");

        try
        {
            var token = GenerateToken();
            var inv = Invitation.Create(_current.OrganisationId.Value, email, role, _current.UserId.Value,
                req.DepartmentId, TimeSpan.FromDays(7), token);
            _db.Invitations.Add(inv);
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync("member.invited", "Invitation", inv.Id, null, new { inv.Email, Role = inv.Role.ToString(), inv.DepartmentId }, ct);

            var deptName = req.DepartmentId is null ? null : await _db.Departments.Where(d => d.Id == req.DepartmentId).Select(d => d.Name).FirstOrDefaultAsync(ct);
            return Result<InvitationDto>.Success(new InvitationDto(inv.Id, inv.Email, inv.Role.ToString(), inv.DepartmentId, deptName, inv.Status.ToString(), inv.ExpiresAt, inv.Token));
        }
        catch (DomainException ex)
        {
            return Result<InvitationDto>.Validation(ex.Message);
        }
    }

    public async Task<Result> RevokeInvitationAsync(Guid id, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.MemberInvite))
            return Result.Forbidden();

        var inv = await _db.Invitations.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (inv is null) return Result.NotFound();
        if (inv.Status != InvitationStatus.Pending) return Result.Validation("Only pending invitations can be revoked");

        _db.Invitations.Remove(inv);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("member.invitation_revoked", "Invitation", id, new { inv.Email, Role = inv.Role.ToString() }, null, ct);
        return Result.Success();
    }

    public async Task<Result<MemberDto>> ChangeRoleAsync(Guid memberId, ChangeRoleRequest req, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result<MemberDto>.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.MemberManage))
            return Result<MemberDto>.Forbidden();

        if (!Enum.TryParse<MemberRole>(req.Role, true, out var newRole))
            return Result<MemberDto>.Validation("Invalid role");
        if (newRole == MemberRole.Owner) return Result<MemberDto>.Validation("Cannot promote to Owner");

        var member = await _db.OrganisationMembers.FirstOrDefaultAsync(m => m.Id == memberId, ct);
        if (member is null) return Result<MemberDto>.NotFound();
        if (member.Role == MemberRole.Owner) return Result<MemberDto>.Validation("Cannot change the Owner's role");

        var oldRole = member.Role.ToString();
        member.ChangeRole(newRole);
        member.AssignDepartment(req.DepartmentId);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("member.role_changed", "OrganisationMember", member.Id,
            new { Role = oldRole, member.DepartmentId }, new { Role = member.Role.ToString(), DepartmentId = req.DepartmentId }, ct);

        var dtoList = await ListAsync(ct);
        var dto = dtoList.Value!.First(m => m.Id == memberId);
        return Result<MemberDto>.Success(dto);
    }

    public async Task<Result> RemoveAsync(Guid memberId, CancellationToken ct = default)
    {
        if (!_current.IsAuthenticated) return Result.Unauthorized();
        if (_current.Role is null || !_perms.Can(_current.Role.Value, Permissions.MemberManage))
            return Result.Forbidden();

        var member = await _db.OrganisationMembers.FirstOrDefaultAsync(m => m.Id == memberId, ct);
        if (member is null) return Result.NotFound();
        if (member.Role == MemberRole.Owner) return Result.Validation("Cannot remove the Owner");

        member.Deactivate();
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("member.removed", "OrganisationMember", member.Id, new { Role = member.Role.ToString() }, null, ct);
        return Result.Success();
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
