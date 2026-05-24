using FinanceTracker.Domain;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace FinanceTracker.UnitTests.Domain;

public class InvitationTests
{
    private static Invitation NewInv(TimeSpan? ttl = null, MemberRole role = MemberRole.Member)
        => Invitation.Create(
            Guid.NewGuid(), "alice@example.com", role,
            Guid.NewGuid(), null, ttl ?? TimeSpan.FromDays(7), "test-token-12345");

    [Fact]
    public void Create_NormalisesEmailToLowercase()
    {
        var inv = Invitation.Create(Guid.NewGuid(), "  ALICE@Example.com ", MemberRole.Member,
            Guid.NewGuid(), null, TimeSpan.FromDays(1), "tok");
        inv.Email.Should().Be("alice@example.com");
        inv.Status.Should().Be(InvitationStatus.Pending);
    }

    [Fact]
    public void Create_Rejects_OwnerRole()
    {
        Action act = () => NewInv(role: MemberRole.Owner);
        act.Should().Throw<DomainException>().WithMessage("*Owner*");
    }

    [Fact]
    public void Accept_Transitions_To_Accepted()
    {
        var inv = NewInv();
        inv.Accept();
        inv.Status.Should().Be(InvitationStatus.Accepted);
        inv.AcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public void Accept_Twice_Fails()
    {
        var inv = NewInv();
        inv.Accept();
        Action act = () => inv.Accept();
        act.Should().Throw<DomainException>().WithMessage("*not pending*");
    }

    [Fact]
    public void Accept_AfterExpiry_Fails()
    {
        var inv = NewInv(ttl: TimeSpan.FromMilliseconds(1));
        Thread.Sleep(50);
        Action act = () => inv.Accept();
        act.Should().Throw<DomainException>().WithMessage("*expired*");
    }

    [Fact]
    public void IsValid_FalseAfterExpiry()
    {
        var inv = NewInv(ttl: TimeSpan.FromMilliseconds(1));
        Thread.Sleep(50);
        inv.IsValid().Should().BeFalse();
    }
}
