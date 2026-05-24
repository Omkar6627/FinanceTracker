using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

public class Budget
{
    public Guid Id { get; private set; }
    public Guid OrganisationId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid OwnedByUserId { get; private set; }
    public decimal LimitAmount { get; private set; }
    public BudgetPeriod Period { get; private set; }
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset? EndDate { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Budget() { }

    public static Budget Create(
        Guid organisationId,
        Guid ownedByUserId,
        Guid categoryId,
        decimal limitAmount,
        BudgetPeriod period,
        DateTimeOffset startDate,
        DateTimeOffset? endDate = null,
        Guid? departmentId = null)
    {
        if (organisationId == Guid.Empty) throw new DomainException("OrganisationId required");
        if (categoryId == Guid.Empty) throw new DomainException("Category required");
        if (limitAmount <= 0m) throw new DomainException("Limit must be greater than zero");
        if (period == BudgetPeriod.Custom && endDate is null)
            throw new DomainException("Custom-period budgets need an end date");
        if (endDate.HasValue && endDate.Value <= startDate)
            throw new DomainException("End date must be after start date");

        return new Budget
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            OwnedByUserId = ownedByUserId,
            CategoryId = categoryId,
            DepartmentId = departmentId,
            LimitAmount = decimal.Round(limitAmount, 2),
            Period = period,
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(decimal limitAmount, BudgetPeriod period, DateTimeOffset startDate, DateTimeOffset? endDate)
    {
        if (limitAmount <= 0m) throw new DomainException("Limit must be greater than zero");
        if (period == BudgetPeriod.Custom && endDate is null)
            throw new DomainException("Custom-period budgets need an end date");
        if (endDate.HasValue && endDate.Value <= startDate)
            throw new DomainException("End date must be after start date");
        LimitAmount = decimal.Round(limitAmount, 2);
        Period = period;
        StartDate = startDate;
        EndDate = endDate;
    }
}
