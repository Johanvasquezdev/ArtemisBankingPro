using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ABP.Infraestructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3FintechPremium : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                schema: "artemisBankingPro",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFrozen",
                schema: "artemisBankingPro",
                table: "SavingsAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFrozen",
                schema: "artemisBankingPro",
                table: "CreditCards",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SavingsGoals",
                schema: "artemisBankingPro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SavingsAccountId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TargetAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AutoRoundupEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ColorHex = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavingsGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavingsGoals_SavingsAccounts_SavingsAccountId",
                        column: x => x.SavingsAccountId,
                        principalSchema: "artemisBankingPro",
                        principalTable: "SavingsAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledPayments",
                schema: "artemisBankingPro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SavingsAccountId = table.Column<int>(type: "integer", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    ContractNumber = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    ExecutionDay = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledPayments_SavingsAccounts_SavingsAccountId",
                        column: x => x.SavingsAccountId,
                        principalSchema: "artemisBankingPro",
                        principalTable: "SavingsAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VirtualCards",
                schema: "artemisBankingPro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SavingsAccountId = table.Column<int>(type: "integer", nullable: false),
                    CardNumber = table.Column<string>(type: "text", nullable: false),
                    CVV = table.Column<string>(type: "text", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LimitAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsFrozen = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VirtualCards_SavingsAccounts_SavingsAccountId",
                        column: x => x.SavingsAccountId,
                        principalSchema: "artemisBankingPro",
                        principalTable: "SavingsAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavingsGoals_SavingsAccountId",
                schema: "artemisBankingPro",
                table: "SavingsGoals",
                column: "SavingsAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPayments_SavingsAccountId",
                schema: "artemisBankingPro",
                table: "ScheduledPayments",
                column: "SavingsAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_VirtualCards_SavingsAccountId",
                schema: "artemisBankingPro",
                table: "VirtualCards",
                column: "SavingsAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavingsGoals",
                schema: "artemisBankingPro");

            migrationBuilder.DropTable(
                name: "ScheduledPayments",
                schema: "artemisBankingPro");

            migrationBuilder.DropTable(
                name: "VirtualCards",
                schema: "artemisBankingPro");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "artemisBankingPro",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsFrozen",
                schema: "artemisBankingPro",
                table: "SavingsAccounts");

            migrationBuilder.DropColumn(
                name: "IsFrozen",
                schema: "artemisBankingPro",
                table: "CreditCards");
        }
    }
}
