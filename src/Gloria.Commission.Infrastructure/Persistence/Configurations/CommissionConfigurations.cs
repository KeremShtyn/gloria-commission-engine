using Gloria.Commission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloria.Commission.Infrastructure.Persistence.Configurations;

public class CommissionRuleConfiguration : IEntityTypeConfiguration<CommissionRule>
{
    public void Configure(EntityTypeBuilder<CommissionRule> b)
    {
        b.ToTable("commission_rules");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.DepartmentCode).HasMaxLength(50);
        b.Property(x => x.ProductGroup).HasMaxLength(50);
        b.Property(x => x.ProductCode).HasMaxLength(50);
        b.Property(x => x.Hotel).HasMaxLength(10);

        b.Property(x => x.Rate).HasPrecision(9, 6);
        b.Property(x => x.FixedAmount).HasPrecision(18, 2);

        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_commission_rules_code");
        b.HasIndex(x => new { x.IsActive, x.EffectiveFrom })
            .HasDatabaseName("idx_commission_rules_is_active_effective_from");

        b.HasMany(x => x.Tiers).WithOne(x => x.CommissionRule)
            .HasForeignKey(x => x.CommissionRuleId).OnDelete(DeleteBehavior.Cascade);

        b.Ignore(x => x.Specificity);
    }
}

public class CommissionRuleTierConfiguration : IEntityTypeConfiguration<CommissionRuleTier>
{
    public void Configure(EntityTypeBuilder<CommissionRuleTier> b)
    {
        b.ToTable("commission_rule_tiers");
        b.HasKey(x => x.Id);
        b.Property(x => x.MinAmount).HasPrecision(18, 2);
        b.Property(x => x.MaxAmount).HasPrecision(18, 2);
        b.Property(x => x.Rate).HasPrecision(9, 6);
        b.HasIndex(x => new { x.CommissionRuleId, x.MinAmount })
            .HasDatabaseName("idx_commission_rule_tiers_rule_id_min_amount");
    }
}

public class CommissionResultConfiguration : IEntityTypeConfiguration<CommissionResult>
{
    public void Configure(EntityTypeBuilder<CommissionResult> b)
    {
        b.ToTable("commission_results");
        b.HasKey(x => x.Id);
        b.Property(x => x.TotalSalesBase).HasPrecision(18, 2);
        b.Property(x => x.TotalCommission).HasPrecision(18, 2);
        b.Property(x => x.CalculatedBy).HasMaxLength(100).IsRequired();

        // Bir personelin bir donemde tek gecerli sonucu olur; yeniden hesap uzerine yazar.
        b.HasIndex(x => new { x.PeriodId, x.EmployeeId }).IsUnique()
            .HasDatabaseName("uq_commission_results_period_id_employee_id");

        b.HasOne(x => x.Period).WithMany().HasForeignKey(x => x.PeriodId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Lines).WithOne(x => x.CommissionResult)
            .HasForeignKey(x => x.CommissionResultId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CommissionResultLineConfiguration : IEntityTypeConfiguration<CommissionResultLine>
{
    public void Configure(EntityTypeBuilder<CommissionResultLine> b)
    {
        b.ToTable("commission_result_lines");
        b.HasKey(x => x.Id);
        b.Property(x => x.RuleCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.RuleType).HasMaxLength(30).IsRequired();
        b.Property(x => x.Explanation).HasMaxLength(500).IsRequired();
        b.Property(x => x.BaseAmount).HasPrecision(18, 2);
        b.Property(x => x.AppliedRate).HasPrecision(9, 6);
        b.Property(x => x.AppliedFixedAmount).HasPrecision(18, 2);
        b.Property(x => x.CommissionAmount).HasPrecision(18, 2);

        b.HasOne(x => x.SaleRecord).WithMany()
            .HasForeignKey(x => x.SaleRecordId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CommissionRule).WithMany()
            .HasForeignKey(x => x.CommissionRuleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(50).IsRequired();
        b.Property(x => x.ChangedBy).HasMaxLength(100).IsRequired();
        b.Property(x => x.ChangedByRole).HasMaxLength(30).IsRequired();
        b.HasIndex(x => new { x.EntityName, x.EntityId })
            .HasDatabaseName("idx_audit_logs_entity_name_entity_id");
        b.HasIndex(x => x.ChangedAtUtc).HasDatabaseName("idx_audit_logs_changed_at_utc");
    }
}
