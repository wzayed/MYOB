using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MYOB.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierCreditGroupIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SupplierCredits_SourcePaymentGroupId",
                table: "SupplierCredits",
                column: "SourcePaymentGroupId",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupplierCredits_SourcePaymentGroupId",
                table: "SupplierCredits");
        }
    }
}
