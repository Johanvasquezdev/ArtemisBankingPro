using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ABP.Infraestructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPersistenceMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "artemisBank");

            migrationBuilder.CreateTable(
                name: "Beneficiaries",
                schema: "artemisBank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountNumber = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beneficiaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Commerces",
                schema: "artemisBank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Logo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commerces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditCards",
                schema: "artemisBank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CardNumber = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreditLimit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpirationDate = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    AmountOwed = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    CVCHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClientId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AssignedByAdminId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Loans",
                schema: "artemisBank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LoanNumber = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AnualInterestRate = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    TermInMonths = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClientId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AssignedByAdminId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavingsAccounts",
                schema: "artemisBank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountNumber = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedByAdminId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavingsAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditCardConsumptions",
                schema: "artemisBank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CommerceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreditCardId = table.Column<int>(type: "integer", nullable: false),
                    CommerceId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardConsumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCardConsumptions_Commerces_CommerceId",
                        column: x => x.CommerceId,
                        principalSchema: "artemisBank",
                        principalTable: "Commerces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CreditCardConsumptions_CreditCards_CreditCardId",
                        column: x => x.CreditCardId,
                        principalSchema: "artemisBank",
                        principalTable: "CreditCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanInstallments",
                schema: "artemisBank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InstallmentAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsOverdue = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    InstallmentNumber = table.Column<int>(type: "integer", nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanInstallments_Loans_LoanId",
                        column: x => x.LoanId,
                        principalSchema: "artemisBank",
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                schema: "artemisBank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Beneficiary = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Origin = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SavingAccountId = table.Column<int>(type: "integer", nullable: false),
                    SourceAccountNumber = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    DestinationAccountNumber = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_SavingsAccounts_SavingAccountId",
                        column: x => x.SavingAccountId,
                        principalSchema: "artemisBank",
                        principalTable: "SavingsAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiaries_OwnerId_AccountNumber",
                schema: "artemisBank",
                table: "Beneficiaries",
                columns: new[] { "OwnerId", "AccountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardConsumptions_CommerceId",
                schema: "artemisBank",
                table: "CreditCardConsumptions",
                column: "CommerceId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardConsumptions_CreditCardId",
                schema: "artemisBank",
                table: "CreditCardConsumptions",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCards_CardNumber",
                schema: "artemisBank",
                table: "CreditCards",
                column: "CardNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanInstallments_LoanId",
                schema: "artemisBank",
                table: "LoanInstallments",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_LoanNumber",
                schema: "artemisBank",
                table: "Loans",
                column: "LoanNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavingsAccounts_AccountNumber",
                schema: "artemisBank",
                table: "SavingsAccounts",
                column: "AccountNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SavingAccountId",
                schema: "artemisBank",
                table: "Transactions",
                column: "SavingAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Beneficiaries",
                schema: "artemisBank");

            migrationBuilder.DropTable(
                name: "CreditCardConsumptions",
                schema: "artemisBank");

            migrationBuilder.DropTable(
                name: "LoanInstallments",
                schema: "artemisBank");

            migrationBuilder.DropTable(
                name: "Transactions",
                schema: "artemisBank");

            migrationBuilder.DropTable(
                name: "Commerces",
                schema: "artemisBank");

            migrationBuilder.DropTable(
                name: "CreditCards",
                schema: "artemisBank");

            migrationBuilder.DropTable(
                name: "Loans",
                schema: "artemisBank");

            migrationBuilder.DropTable(
                name: "SavingsAccounts",
                schema: "artemisBank");
        }
    }
}
