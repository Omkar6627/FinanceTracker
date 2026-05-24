using FinanceTracker.Application.Abstractions;
using FinanceTracker.Application.Common;
using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly ICurrentUser _current;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser current) : base(options)
    {
        _current = current;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<OrganisationMember> OrganisationMembers => Set<OrganisationMember>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        });

        b.Entity<Organisation>(e =>
        {
            e.ToTable("organisations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.Mode).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<OrganisationMember>(e =>
        {
            e.ToTable("organisation_members");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganisationId, x.UserId }).IsUnique();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.HasQueryFilter(m => _current.OrganisationId == null || m.OrganisationId == _current.OrganisationId);
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Icon).HasMaxLength(60);
            e.Property(x => x.Color).HasMaxLength(20);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.OrganisationId, x.Name, x.Type });
            e.HasQueryFilter(c => _current.OrganisationId == null || c.OrganisationId == _current.OrganisationId);
        });

        b.Entity<Account>(e =>
        {
            e.ToTable("accounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Balance).HasColumnType("decimal(15,2)");
            e.HasQueryFilter(a => _current.OrganisationId == null || a.OrganisationId == _current.OrganisationId);
        });

        b.Entity<Transaction>(e =>
        {
            e.ToTable("transactions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("decimal(15,2)");
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Source).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.OrganisationId, x.Date });
            e.HasIndex(x => new { x.OrganisationId, x.Status });
            e.HasIndex(x => x.ExternalRef);
            e.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(t => _current.OrganisationId == null || t.OrganisationId == _current.OrganisationId);
        });

        b.Entity<Budget>(e =>
        {
            e.ToTable("budgets");
            e.HasKey(x => x.Id);
            e.Property(x => x.LimitAmount).HasColumnType("decimal(15,2)");
            e.Property(x => x.Period).HasConversion<string>().HasMaxLength(20);
            e.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(bd => _current.OrganisationId == null || bd.OrganisationId == _current.OrganisationId);
        });

        b.Entity<Department>(e =>
        {
            e.ToTable("departments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.OrganisationId, x.Name }).IsUnique();
            e.HasQueryFilter(d => _current.OrganisationId == null || d.OrganisationId == _current.OrganisationId);
        });

        b.Entity<Invitation>(e =>
        {
            e.ToTable("invitations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Token).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => new { x.OrganisationId, x.Email });
            e.HasQueryFilter(i => _current.OrganisationId == null || i.OrganisationId == _current.OrganisationId);
        });

        b.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(60).IsRequired();
            e.HasIndex(x => new { x.OrganisationId, x.OccurredAt });
            e.HasQueryFilter(a => _current.OrganisationId == null || a.OrganisationId == _current.OrganisationId);
        });
    }
}
