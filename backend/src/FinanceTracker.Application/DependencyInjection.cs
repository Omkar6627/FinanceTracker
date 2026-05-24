using FinanceTracker.Application.Common;
using FinanceTracker.Application.Features.Auth;
using FinanceTracker.Application.Features.Audit;
using FinanceTracker.Application.Features.Budgets;
using FinanceTracker.Application.Features.Categories;
using FinanceTracker.Application.Features.Departments;
using FinanceTracker.Application.Features.Members;
using FinanceTracker.Application.Features.Organisations;
using FinanceTracker.Application.Features.Reports;
using FinanceTracker.Application.Features.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IOrganisationService, OrganisationService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        return services;
    }
}
