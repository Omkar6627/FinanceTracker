using FinanceTracker.Domain;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace FinanceTracker.UnitTests.Domain;

public class BudgetTests
{
    [Fact]
    public void Create_Monthly_Succeeds()
    {
        var b = Budget.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 500m, BudgetPeriod.Monthly, DateTimeOffset.UtcNow);
        b.LimitAmount.Should().Be(500m);
        b.Period.Should().Be(BudgetPeriod.Monthly);
    }

    [Fact]
    public void Create_CustomWithoutEnd_Throws()
    {
        Action act = () => Budget.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, BudgetPeriod.Custom, DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_NonPositiveLimit_Throws()
    {
        Action act = () => Budget.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, BudgetPeriod.Monthly, DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_EndBeforeStart_Throws()
    {
        var start = DateTimeOffset.UtcNow;
        Action act = () => Budget.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, BudgetPeriod.Custom, start, start.AddDays(-1));
        act.Should().Throw<DomainException>();
    }
}

public class OrganisationTests
{
    [Fact]
    public void CreateIndividual_DefaultsToINR()
    {
        var o = Organisation.CreateIndividual("Alice");
        o.Mode.Should().Be(OrganisationMode.Individual);
        o.Currency.Should().Be("INR");
        o.Name.Should().Contain("Alice");
    }

    [Fact]
    public void CreateEnterprise_NormalisesCurrency()
    {
        var o = Organisation.CreateEnterprise("Acme", "usd");
        o.Currency.Should().Be("USD");
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("DOLLAR")]
    public void Create_InvalidCurrency_Throws(string c)
    {
        Action act = () => Organisation.CreateEnterprise("Acme", c);
        act.Should().Throw<DomainException>();
    }
}

public class UserTests
{
    [Fact]
    public void Create_LowercasesEmail()
    {
        var u = User.Create("DEMO@EXAMPLE.COM", "hash", "Demo");
        u.Email.Should().Be("demo@example.com");
    }
}

public class CategoryTests
{
    [Fact]
    public void Create_AppliesDefaults()
    {
        var c = Category.Create(Guid.NewGuid(), "Food", "", "", CategoryType.Expense);
        c.Icon.Should().NotBeNullOrEmpty();
        c.Color.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Update_SystemCategory_Throws()
    {
        var c = Category.Create(Guid.NewGuid(), "Food", "icon", "#fff", CategoryType.Expense, isSystem: true);
        Action act = () => c.Update("X", "i", "#000");
        act.Should().Throw<DomainException>();
    }
}
