using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MYOB.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierCreditCarryForward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CreditAmount",
                table: "SupplierPayments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierCreditId",
                table: "SupplierPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupplierCredits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceSupplierReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcePaymentGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    PaymentMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierCredits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierCredits_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierCredits_SupplierReceipts_SourceSupplierReceiptId",
                        column: x => x.SourceSupplierReceiptId,
                        principalTable: "SupplierReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierCredits_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_SupplierCreditId",
                table: "SupplierPayments",
                column: "SupplierCreditId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCredits_PaymentMethodId",
                table: "SupplierCredits",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCredits_SourceSupplierReceiptId",
                table: "SupplierCredits",
                column: "SourceSupplierReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierCredits_SupplierId_RemainingAmount",
                table: "SupplierCredits",
                columns: new[] { "SupplierId", "RemainingAmount" });

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_SupplierCredits_SupplierCreditId",
                table: "SupplierPayments",
                column: "SupplierCreditId",
                principalTable: "SupplierCredits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_SupplierCredits_SupplierCreditId",
                table: "SupplierPayments");

            migrationBuilder.DropTable(
                name: "SupplierCredits");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_SupplierCreditId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "CreditAmount",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "SupplierCreditId",
                table: "SupplierPayments");
        }
    }
}
