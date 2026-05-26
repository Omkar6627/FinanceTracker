using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using FinanceTracker.Infrastructure.Persistence;
using FinanceTracker.Infrastructure.Security;
using FinanceTracker.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtOptions>(config.GetSection("Jwt"));

        var useInMemory = config.GetValue<bool>("Database:UseInMemory");
        var connection = config.GetConnectionString("Postgres");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (useInMemory || string.IsNullOrWhiteSpace(connection))
            {
                options.UseInMemoryDatabase("FinanceTrackerDev");
            }
            else
            {
                options.UseNpgsql(connection, npg => npg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            }
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUnitOfWork>(sp => new EfUnitOfWork(sp.GetRequiredService<AppDbContext>()));
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ISeedService, SeedService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IRecurringMaterializer, RecurringMaterializer>();
        services.AddScoped<IReportPdfService, ReportPdfService>();
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        services.AddHttpContextAccessor();

        services.AddHttpClient<IFxRateService, FrankfurterFxService>(c =>
        {
            c.BaseAddress = new Uri("https://api.frankfurter.app/");
            c.Timeout = TimeSpan.FromSeconds(8);
        });

        if (config.GetValue("Recurring:Enabled", true))
        {
            var intervalSeconds = config.GetValue("Recurring:IntervalSeconds", 3600);
            services.AddHostedService(sp => new RecurringWorker(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RecurringWorker>>(),
                TimeSpan.FromSeconds(Math.Max(15, intervalSeconds))));
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var jwt = new JwtOptions();
                config.GetSection("Jwt").Bind(jwt);
                var temp = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(jwt));
                options.TokenValidationParameters = temp.BuildValidationParameters();
                options.MapInboundClaims = false;
            });

        services.AddAuthorization();

        return services;
    }

    private sealed class EfUnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;
        public EfUnitOfWork(AppDbContext db) { _db = db; }
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
    }
}
