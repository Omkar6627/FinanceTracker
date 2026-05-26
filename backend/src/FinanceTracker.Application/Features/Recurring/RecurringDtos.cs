namespace FinanceTracker.Application.Features.Recurring;

public record RecurringDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    Guid? AccountId,
    string? AccountName,
    decimal Amount,
    string Type,
    string Note,
    string Frequency,
    DateTimeOffset StartDate,
    DateTimeOffset NextRunDate,
    DateTimeOffset? EndDate,
    bool IsActive,
    DateTimeOffset? LastRunAt);

public record CreateRecurringRequest(
    Guid CategoryId,
    Guid? AccountId,
    decimal Amount,
    string Type,
    string? Note,
    string Frequency,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate);

public record UpdateRecurringRequest(
    Guid CategoryId,
    Guid? AccountId,
    decimal Amount,
    string Type,
    string? Note,
    string Frequency,
    DateTimeOffset? EndDate,
    bool IsActive);
