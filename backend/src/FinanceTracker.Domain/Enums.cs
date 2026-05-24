namespace FinanceTracker.Domain;

public enum OrganisationMode
{
    Individual = 0,
    Enterprise = 1
}

public enum MemberRole
{
    Owner = 0,
    Admin = 1,
    Manager = 2,
    Member = 3,
    Viewer = 4
}

public enum TransactionType
{
    Income = 0,
    Expense = 1,
    Transfer = 2
}

public enum TransactionStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    AutoApproved = 4
}

public enum TransactionSource
{
    Manual = 0,
    BankImport = 1,
    Csv = 2
}

public enum CategoryType
{
    Income = 0,
    Expense = 1
}

public enum AccountType
{
    Savings = 0,
    Current = 1,
    Credit = 2,
    Wallet = 3,
    Corporate = 4
}

public enum BudgetPeriod
{
    Weekly = 0,
    Monthly = 1,
    Custom = 2
}

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Expired = 2,
    Revoked = 3
}
