namespace FinanceTracker.Application.Features.Budgets;

public record BudgetDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    decimal LimitAmount,
    string Period,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate);

public record BudgetStatusDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    decimal LimitAmount,
    decimal SpentAmount,
    decimal RemainingAmount,
    decimal PercentUsed,
    string Period,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd);

public record CreateBudgetRequest(
    Guid CategoryId,
    decimal LimitAmount,
    string Period,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate);

public record UpdateBudgetRequest(
    decimal LimitAmount,
    string Period,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate);
