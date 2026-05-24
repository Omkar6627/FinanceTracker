namespace FinanceTracker.Application.Features.Transactions;

public record TransactionDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    Guid? AccountId,
    string? AccountName,
    decimal Amount,
    string Type,
    string Status,
    string Note,
    DateTimeOffset Date,
    string Source);

public record TransactionListItem(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    decimal Amount,
    string Type,
    string Status,
    string Note,
    DateTimeOffset Date,
    Guid SubmittedByUserId,
    string SubmittedByName);

public record CreateTransactionRequest(
    Guid CategoryId,
    Guid? AccountId,
    decimal Amount,
    string Type,
    string? Note,
    DateTimeOffset Date);

public record UpdateTransactionRequest(
    Guid CategoryId,
    Guid? AccountId,
    decimal Amount,
    string Type,
    string? Note,
    DateTimeOffset Date);

public record TransactionListResponse(
    IReadOnlyList<TransactionListItem> Items,
    int Page,
    int PageSize,
    int Total);

public record TransactionQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    Guid? CategoryId,
    string? Type,
    string? Status = null,
    int Page = 1,
    int PageSize = 25);

public record RejectTransactionRequest(string Reason);
