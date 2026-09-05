using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MYOB.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentGrouping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentGroupId",
                table: "SupplierPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentGroupId",
                table: "CustomerPayments",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentGroupId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "PaymentGroupId",
                table: "CustomerPayments");
        }
    }
}
