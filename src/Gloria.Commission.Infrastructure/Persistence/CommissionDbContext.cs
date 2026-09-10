using System.Reflection;
using Gloria.Commission.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Commission.Infrastructure.Persistence;

public class CommissionDbContext : DbContext
{
    public CommissionDbContext(DbContextOptions<CommissionDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<ProductGroup> ProductGroups => Set<ProductGroup>();
    public DbSet<Period> Periods => Set<Period>();
    public DbSet<SaleRecord> SaleRecords => Set<SaleRecord>();
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
    public DbSet<CommissionRuleTier> CommissionRuleTiers => Set<CommissionRuleTier>();
    public DbSet<CommissionResult> CommissionResults => Set<CommissionResult>();
    public DbSet<CommissionResultLine> CommissionResultLines => Set<CommissionResultLine>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<StagingRow> StagingRows => Set<StagingRow>();
    public DbSet<ImportError> ImportErrors => Set<ImportError>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
