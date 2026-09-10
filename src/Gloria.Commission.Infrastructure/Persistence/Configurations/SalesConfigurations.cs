using Gloria.Commission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloria.Commission.Infrastructure.Persistence.Configurations;

public class SaleRecordConfiguration : IEntityTypeConfiguration<SaleRecord>
{
    public void Configure(EntityTypeBuilder<SaleRecord> b)
    {
        b.ToTable("sale_records");
        b.HasKey(x => x.Id);

        b.Property(x => x.SourceDocumentNo).HasMaxLength(50).IsRequired();
        b.Property(x => x.SourceHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.EmployeeNo).HasMaxLength(20).IsRequired();
        b.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.ProductName).HasMaxLength(150).IsRequired();
        b.Property(x => x.ProductGroup).HasMaxLength(50).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.Property(x => x.Hotel).HasMaxLength(10);
        b.Property(x => x.Outlet).HasMaxLength(20);
        b.Property(x => x.RoomNo).HasMaxLength(20);
        b.Property(x => x.SourceReference).HasMaxLength(50);

        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.AmountTry).HasPrecision(18, 2);
        b.Property(x => x.ExchangeRate).HasPrecision(18, 6);

        // Mukerrer kayit engelleme: ayni kaynak satiri ikinci kez yazilamaz.
        b.HasIndex(x => x.SourceHash).IsUnique().HasDatabaseName("uq_sale_records_source_hash");

        // Prim hesabinin ana sorgusu: personel + tarih araligi.
        b.HasIndex(x => new { x.EmployeeNo, x.TransactionDate })
            .HasDatabaseName("idx_sale_records_employee_no_transaction_date");
        b.HasIndex(x => x.TransactionDate).HasDatabaseName("idx_sale_records_transaction_date");

        b.HasOne(x => x.Employee).WithMany(x => x.Sales)
            .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ReversedSale).WithMany()
            .HasForeignKey(x => x.ReversedSaleId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ImportBatch).WithMany()
            .HasForeignKey(x => x.ImportBatchId).OnDelete(DeleteBehavior.Restrict);

        b.Ignore(x => x.IsCommissionable);
    }
}

public class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> b)
    {
        b.ToTable("import_batches");
        b.HasKey(x => x.Id);
        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.FileHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.ImportedBy).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.FileHash).HasDatabaseName("idx_import_batches_file_hash");
    }
}

public class ImportErrorConfiguration : IEntityTypeConfiguration<ImportError>
{
    public void Configure(EntityTypeBuilder<ImportError> b)
    {
        b.ToTable("import_errors");
        b.HasKey(x => x.Id);
        b.Property(x => x.RawLine).HasMaxLength(2000).IsRequired();
        b.Property(x => x.ErrorCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.ErrorMessage).HasMaxLength(500).IsRequired();
        b.HasOne(x => x.ImportBatch).WithMany(x => x.Errors)
            .HasForeignKey(x => x.ImportBatchId).OnDelete(DeleteBehavior.Cascade);
    }
}
