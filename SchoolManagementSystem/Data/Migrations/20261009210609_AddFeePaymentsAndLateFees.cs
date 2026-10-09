using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddFeePaymentsAndLateFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultDueDay",
                table: "SchoolSettings",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<decimal>(
                name: "LateFeeAmount",
                table: "SchoolSettings",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "LateFeeGraceDays",
                table: "SchoolSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "LateFee",
                table: "FeeVouchers",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "FeePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FeeVoucherId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaidOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CollectedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeePayments_FeeVouchers_FeeVoucherId",
                        column: x => x.FeeVoucherId,
                        principalTable: "FeeVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_FeeVoucherId",
                table: "FeePayments",
                column: "FeeVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_PaidOn",
                table: "FeePayments",
                column: "PaidOn");

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_ReceiptNumber",
                table: "FeePayments",
                column: "ReceiptNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeePayments");

            migrationBuilder.DropColumn(
                name: "DefaultDueDay",
                table: "SchoolSettings");

            migrationBuilder.DropColumn(
                name: "LateFeeAmount",
                table: "SchoolSettings");

            migrationBuilder.DropColumn(
                name: "LateFeeGraceDays",
                table: "SchoolSettings");

            migrationBuilder.DropColumn(
                name: "LateFee",
                table: "FeeVouchers");
        }
    }
}
