namespace FinanceTracker.Application.Features.Reports;

public record DashboardSummary(
    decimal IncomeMonth,
    decimal ExpenseMonth,
    decimal NetMonth,
    int TransactionCountMonth,
    decimal IncomeToday,
    decimal ExpenseToday,
    IReadOnlyList<CategorySlice> TopCategories,
    IReadOnlyList<RecentTransaction> RecentTransactions);

public record CategorySlice(
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    decimal Amount,
    decimal Percent);

public record RecentTransaction(
    Guid Id,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    decimal Amount,
    string Type,
    string Note,
    DateTimeOffset Date);

public record MonthlyReport(
    int Year,
    int Month,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Net,
    IReadOnlyList<DayPoint> DailySeries,
    IReadOnlyList<CategorySlice> ExpenseByCategory,
    IReadOnlyList<CategorySlice> IncomeByCategory);

public record DayPoint(DateTime Date, decimal Income, decimal Expense);

public record TrendPoint(int Year, int Month, decimal Income, decimal Expense, decimal Net);

public record TrendReport(IReadOnlyList<TrendPoint> Points);

public record DepartmentSummary(
    Guid? DepartmentId,
    string DepartmentName,
    decimal Income,
    decimal Expense,
    decimal Net,
    int TransactionCount);
