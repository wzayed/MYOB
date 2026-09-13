using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MYOB.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicPaymentNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicComments",
                table: "SupplierPayments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicComments",
                table: "SupplierCredits",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicComments",
                table: "CustomerPayments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicComments",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "PublicComments",
                table: "SupplierCredits");

            migrationBuilder.DropColumn(
                name: "PublicComments",
                table: "CustomerPayments");
        }
    }
}
