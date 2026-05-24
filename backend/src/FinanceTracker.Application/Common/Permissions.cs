using FinanceTracker.Domain;

namespace FinanceTracker.Application.Common;

public static class Permissions
{
    public const string TransactionCreate = "transaction.create";
    public const string TransactionApprove = "transaction.approve";
    public const string TransactionDelete = "transaction.delete";
    public const string BudgetManage = "budget.manage";
    public const string ReportView = "report.view";
    public const string MemberInvite = "member.invite";
    public const string MemberManage = "member.manage";
    public const string DepartmentManage = "department.manage";
    public const string AuditView = "audit.view";
    public const string SettingsManage = "settings.manage";
    public const string OrganisationModeSwitch = "organisation.mode.switch";
}

public interface IPermissionService
{
    bool Can(MemberRole role, string permission);
}

public class PermissionService : IPermissionService
{
    private static readonly Dictionary<string, HashSet<MemberRole>> _matrix = new()
    {
        [Permissions.TransactionCreate]      = new() { MemberRole.Owner, MemberRole.Admin, MemberRole.Manager, MemberRole.Member },
        [Permissions.TransactionApprove]     = new() { MemberRole.Owner, MemberRole.Admin, MemberRole.Manager },
        [Permissions.TransactionDelete]      = new() { MemberRole.Owner, MemberRole.Admin, MemberRole.Member },
        [Permissions.BudgetManage]           = new() { MemberRole.Owner, MemberRole.Admin, MemberRole.Manager },
        [Permissions.ReportView]             = new() { MemberRole.Owner, MemberRole.Admin, MemberRole.Manager, MemberRole.Member, MemberRole.Viewer },
        [Permissions.MemberInvite]           = new() { MemberRole.Owner, MemberRole.Admin },
        [Permissions.MemberManage]           = new() { MemberRole.Owner, MemberRole.Admin },
        [Permissions.DepartmentManage]       = new() { MemberRole.Owner, MemberRole.Admin },
        [Permissions.AuditView]              = new() { MemberRole.Owner, MemberRole.Admin },
        [Permissions.SettingsManage]         = new() { MemberRole.Owner, MemberRole.Admin },
        [Permissions.OrganisationModeSwitch] = new() { MemberRole.Owner },
    };

    public bool Can(MemberRole role, string permission)
        => _matrix.TryGetValue(permission, out var allowed) && allowed.Contains(role);
}
