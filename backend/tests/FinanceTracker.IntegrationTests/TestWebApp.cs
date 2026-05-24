using FinanceTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceTracker.IntegrationTests;

public class TestWebApp : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"Test_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:UseInMemory", "true");
        builder.UseSetting("Jwt:SigningKey", "INTEGRATION-TEST-SIGNING-KEY-AT-LEAST-32-CHARS-LONG");

        builder.ConfigureServices(services =>
        {
            var ctxDesc = services.Single(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(ctxDesc);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}
