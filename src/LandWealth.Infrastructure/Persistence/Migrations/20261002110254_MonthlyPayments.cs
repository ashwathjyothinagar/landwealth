using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LandWealth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MonthlyPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DueDay = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CategoryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TotalInstallments = table.Column<int>(type: "int", nullable: true),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    StoppedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyPayments_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyPayments_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyPayments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "MonthlyPaymentClearings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MonthlyPaymentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TransactionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastModifiedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyPaymentClearings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyPaymentClearings_MonthlyPayments_MonthlyPaymentId",
                        column: x => x.MonthlyPaymentId,
                        principalTable: "MonthlyPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyPaymentClearings_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyPaymentClearings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPaymentClearings_Payment_Month",
                table: "MonthlyPaymentClearings",
                columns: new[] { "MonthlyPaymentId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPaymentClearings_TransactionId",
                table: "MonthlyPaymentClearings",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPaymentClearings_UserId",
                table: "MonthlyPaymentClearings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPayments_AccountId",
                table: "MonthlyPayments",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPayments_CategoryId",
                table: "MonthlyPayments",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPayments_UserId",
                table: "MonthlyPayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPayments_UserId_IsActive",
                table: "MonthlyPayments",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyPaymentClearings");

            migrationBuilder.DropTable(
                name: "MonthlyPayments");
        }
    }
}
