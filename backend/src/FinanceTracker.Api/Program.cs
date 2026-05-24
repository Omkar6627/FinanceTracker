using System.Text.Json.Serialization;
using FinanceTracker.Api.Endpoints;
using FinanceTracker.Api.Middleware;
using FinanceTracker.Application;
using FinanceTracker.Infrastructure;
using FinanceTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FinanceTracker API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(
        "http://localhost:8100", "http://localhost:4200", "http://localhost:8101",
        "http://127.0.0.1:8100", "http://127.0.0.1:4200")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddRouting(o => o.LowercaseUrls = true);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/v1/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }))
    .WithTags("Health").AllowAnonymous();

app.MapAuthEndpoints();
app.MapCategoryEndpoints();
app.MapTransactionEndpoints();
app.MapBudgetEndpoints();
app.MapReportEndpoints();
app.MapOrganisationEndpoints();
app.MapDepartmentEndpoints();
app.MapMemberEndpoints();
app.MapAuditEndpoints();

app.Run();

public partial class Program { }
