using FinanceTracker.Application.Common;
using FinanceTracker.Domain;
using FluentAssertions;
using Xunit;

namespace FinanceTracker.UnitTests.Application;

public class PermissionServiceTests
{
    private readonly PermissionService _svc = new();

    [Theory]
    [InlineData(MemberRole.Owner, Permissions.TransactionApprove, true)]
    [InlineData(MemberRole.Admin, Permissions.TransactionApprove, true)]
    [InlineData(MemberRole.Manager, Permissions.TransactionApprove, true)]
    [InlineData(MemberRole.Member, Permissions.TransactionApprove, false)]
    [InlineData(MemberRole.Viewer, Permissions.TransactionApprove, false)]
    public void Approve_OnlyForOwnerAdminManager(MemberRole role, string perm, bool expected)
        => _svc.Can(role, perm).Should().Be(expected);

    [Theory]
    [InlineData(MemberRole.Owner, true)]
    [InlineData(MemberRole.Admin, false)]
    [InlineData(MemberRole.Manager, false)]
    public void ModeSwitch_OnlyOwner(MemberRole role, bool expected)
        => _svc.Can(role, Permissions.OrganisationModeSwitch).Should().Be(expected);

    [Theory]
    [InlineData(MemberRole.Owner, true)]
    [InlineData(MemberRole.Admin, true)]
    [InlineData(MemberRole.Manager, false)]
    [InlineData(MemberRole.Member, false)]
    [InlineData(MemberRole.Viewer, false)]
    public void InviteMembers_OnlyOwnerAdmin(MemberRole role, bool expected)
        => _svc.Can(role, Permissions.MemberInvite).Should().Be(expected);

    [Fact]
    public void UnknownPermission_DeniesAll()
    {
        _svc.Can(MemberRole.Owner, "made.up.permission").Should().BeFalse();
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.Manager)]
    [InlineData(MemberRole.Admin)]
    [InlineData(MemberRole.Owner)]
    public void EveryRoleCanViewReports(MemberRole role)
        => _svc.Can(role, Permissions.ReportView).Should().BeTrue();
}
