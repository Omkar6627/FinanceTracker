using FinanceTracker.Domain;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace FinanceTracker.UnitTests.Domain;

public class TransactionTests
{
    private static Transaction NewExpense(OrganisationMode mode = OrganisationMode.Individual)
        => Transaction.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            amount: 100m, type: TransactionType.Expense, note: "Lunch",
            date: DateTimeOffset.UtcNow, mode: mode);

    [Fact]
    public void Create_Individual_AutoApproves()
    {
        var tx = NewExpense();
        tx.Status.Should().Be(TransactionStatus.AutoApproved);
    }

    [Fact]
    public void Create_Enterprise_StartsPending()
    {
        var tx = NewExpense(OrganisationMode.Enterprise);
        tx.Status.Should().Be(TransactionStatus.PendingApproval);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_RejectsNonPositiveAmount(decimal amount)
    {
        Action act = () => Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            amount, TransactionType.Expense, "x", DateTimeOffset.UtcNow, OrganisationMode.Individual);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Approve_OnApprovedTx_Throws()
    {
        var tx = NewExpense();
        Action act = () => tx.Approve(Guid.NewGuid());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Approve_Pending_Succeeds()
    {
        var tx = NewExpense(OrganisationMode.Enterprise);
        var approver = Guid.NewGuid();
        tx.Approve(approver);
        tx.Status.Should().Be(TransactionStatus.Approved);
        tx.ApprovedByUserId.Should().Be(approver);
        tx.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reject_RequiresReason()
    {
        var tx = NewExpense(OrganisationMode.Enterprise);
        Action act = () => tx.Reject(Guid.NewGuid(), "");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_OfApprovedTx_Throws()
    {
        var tx = NewExpense(OrganisationMode.Enterprise);
        tx.Approve(Guid.NewGuid());
        Action act = () => tx.Update(Guid.NewGuid(), null, 50m, TransactionType.Expense, "y", DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Amount_IsRoundedToTwoDecimals()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            10.567m, TransactionType.Expense, null, DateTimeOffset.UtcNow, OrganisationMode.Individual);
        tx.Amount.Should().Be(10.57m);
    }

    [Fact]
    public void SignedAmount_Income_Positive()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            50m, TransactionType.Income, null, DateTimeOffset.UtcNow, OrganisationMode.Individual);
        tx.SignedAmount().Should().Be(50m);
    }

    [Fact]
    public void SignedAmount_Expense_Negative()
    {
        var tx = NewExpense();
        tx.SignedAmount().Should().Be(-100m);
    }
}
