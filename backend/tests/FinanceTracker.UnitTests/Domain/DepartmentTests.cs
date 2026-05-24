using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace FinanceTracker.UnitTests.Domain;

public class DepartmentTests
{
    [Fact]
    public void Create_TrimsName_AndIsActive()
    {
        var d = Department.Create(Guid.NewGuid(), "  Engineering  ");
        d.Name.Should().Be("Engineering");
        d.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_BlankName_Throws()
    {
        Action act = () => Department.Create(Guid.NewGuid(), "   ");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rename_Trims()
    {
        var d = Department.Create(Guid.NewGuid(), "Old");
        d.Rename(" New ");
        d.Name.Should().Be("New");
    }

    [Fact]
    public void Deactivate_FlagsInactive()
    {
        var d = Department.Create(Guid.NewGuid(), "Ops");
        d.Deactivate();
        d.IsActive.Should().BeFalse();
    }
}
