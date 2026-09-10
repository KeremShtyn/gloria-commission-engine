using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gloria.Commission.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    OldValues = table.Column<string>(type: "TEXT", nullable: true),
                    NewValues = table.Column<string>(type: "TEXT", nullable: true),
                    ChangedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ChangedByRole = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "hotels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "import_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalRows = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportedRows = table.Column<int>(type: "INTEGER", nullable: false),
                    DuplicateRows = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedRows = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "periods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_periods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeNo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HotelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HireDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TerminationDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employees_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employees_hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "import_errors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RowNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RawLine = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_errors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_import_errors_import_batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "staging_rows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    RowNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RawLine = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SaleRecordId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staging_rows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staging_rows_import_batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commission_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    RuleType = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceSystem = table.Column<int>(type: "INTEGER", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProductGroupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    HotelId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Rate = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: true),
                    FixedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    MultiplyByQuantity = table.Column<bool>(type: "INTEGER", nullable: false),
                    TierApplication = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commission_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commission_rules_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commission_rules_hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commission_rules_product_groups_ProductGroupId",
                        column: x => x.ProductGroupId,
                        principalTable: "product_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commission_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TotalSalesBase = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalCommission = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CalculatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commission_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commission_results_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commission_results_periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceDocumentNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SourceHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SourceEmployeeNo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    ProductGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    AmountTry = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ReversedSaleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SourceReference = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    SourceHotel = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Outlet = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    RoomNo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ImportBatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sale_records_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_records_import_batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_records_product_groups_ProductGroupId",
                        column: x => x.ProductGroupId,
                        principalTable: "product_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_records_sale_records_ReversedSaleId",
                        column: x => x.ReversedSaleId,
                        principalTable: "sale_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commission_rule_tiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CommissionRuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MinAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MaxAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Rate = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commission_rule_tiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commission_rule_tiers_commission_rules_CommissionRuleId",
                        column: x => x.CommissionRuleId,
                        principalTable: "commission_rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commission_result_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CommissionResultId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaleRecordId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CommissionRuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RuleType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    BaseAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AppliedRate = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: true),
                    AppliedFixedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Explanation = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    StepOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commission_result_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commission_result_lines_commission_results_CommissionResultId",
                        column: x => x.CommissionResultId,
                        principalTable: "commission_results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_commission_result_lines_commission_rules_CommissionRuleId",
                        column: x => x.CommissionRuleId,
                        principalTable: "commission_rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commission_result_lines_sale_records_SaleRecordId",
                        column: x => x.SaleRecordId,
                        principalTable: "sale_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_changed_at_utc",
                table: "audit_logs",
                column: "ChangedAtUtc");

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_entity_name_entity_id",
                table: "audit_logs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_commission_result_lines_CommissionResultId",
                table: "commission_result_lines",
                column: "CommissionResultId");

            migrationBuilder.CreateIndex(
                name: "IX_commission_result_lines_CommissionRuleId",
                table: "commission_result_lines",
                column: "CommissionRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_commission_result_lines_SaleRecordId",
                table: "commission_result_lines",
                column: "SaleRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_commission_results_EmployeeId",
                table: "commission_results",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "uq_commission_results_period_id_employee_id",
                table: "commission_results",
                columns: new[] { "PeriodId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_commission_rule_tiers_rule_id_min_amount",
                table: "commission_rule_tiers",
                columns: new[] { "CommissionRuleId", "MinAmount" });

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_DepartmentId",
                table: "commission_rules",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_HotelId",
                table: "commission_rules",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_ProductGroupId",
                table: "commission_rules",
                column: "ProductGroupId");

            migrationBuilder.CreateIndex(
                name: "idx_commission_rules_is_active_effective_from",
                table: "commission_rules",
                columns: new[] { "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "uq_commission_rules_code",
                table: "commission_rules",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_departments_code",
                table: "departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_DepartmentId",
                table: "employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_HotelId",
                table: "employees",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "uq_employees_employee_no",
                table: "employees",
                column: "EmployeeNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_hotels_code",
                table: "hotels",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_import_batches_file_hash",
                table: "import_batches",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_import_errors_ImportBatchId",
                table: "import_errors",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "uq_periods_year_month",
                table: "periods",
                columns: new[] { "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_groups_code",
                table: "product_groups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sale_records_ImportBatchId",
                table: "sale_records",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_sale_records_ProductGroupId",
                table: "sale_records",
                column: "ProductGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_sale_records_ReversedSaleId",
                table: "sale_records",
                column: "ReversedSaleId");

            migrationBuilder.CreateIndex(
                name: "idx_sale_records_employee_id_transaction_date",
                table: "sale_records",
                columns: new[] { "EmployeeId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "idx_sale_records_transaction_date",
                table: "sale_records",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "uq_sale_records_source_hash",
                table: "sale_records",
                column: "SourceHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_staging_rows_batch_id_row_number",
                table: "staging_rows",
                columns: new[] { "ImportBatchId", "RowNumber" });

            migrationBuilder.CreateIndex(
                name: "idx_staging_rows_status",
                table: "staging_rows",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "commission_result_lines");

            migrationBuilder.DropTable(
                name: "commission_rule_tiers");

            migrationBuilder.DropTable(
                name: "import_errors");

            migrationBuilder.DropTable(
                name: "staging_rows");

            migrationBuilder.DropTable(
                name: "commission_results");

            migrationBuilder.DropTable(
                name: "sale_records");

            migrationBuilder.DropTable(
                name: "commission_rules");

            migrationBuilder.DropTable(
                name: "periods");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "import_batches");

            migrationBuilder.DropTable(
                name: "product_groups");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "hotels");
        }
    }
}
