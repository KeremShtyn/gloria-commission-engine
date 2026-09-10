using Gloria.Commission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloria.Commission.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.ToTable("departments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_departments_code");
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.ToTable("employees");
        b.HasKey(x => x.Id);
        b.Property(x => x.EmployeeNo).HasMaxLength(20).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Hotel).HasMaxLength(10).IsRequired();
        b.HasIndex(x => x.EmployeeNo).IsUnique().HasDatabaseName("uq_employees_employee_no");
        b.HasOne(x => x.Department).WithMany(x => x.Employees)
            .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("products");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.ProductGroup).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_products_code");
    }
}

public class PeriodConfiguration : IEntityTypeConfiguration<Period>
{
    public void Configure(EntityTypeBuilder<Period> b)
    {
        b.ToTable("periods");
        b.HasKey(x => x.Id);
        b.Property(x => x.ClosedBy).HasMaxLength(100);
        b.HasIndex(x => new { x.Year, x.Month }).IsUnique().HasDatabaseName("uq_periods_year_month");
        b.Ignore(x => x.IsClosed);
    }
}
