using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABP.Infraestructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameArtemisBankSchemaToArtemisBankingPro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "artemisBankingPro");

            migrationBuilder.RenameTable(
                name: "Transactions",
                schema: "artemisBank",
                newName: "Transactions",
                newSchema: "artemisBankingPro");

            migrationBuilder.RenameTable(
                name: "SavingsAccounts",
                schema: "artemisBank",
                newName: "SavingsAccounts",
                newSchema: "artemisBankingPro");

            migrationBuilder.RenameTable(
                name: "Loans",
                schema: "artemisBank",
                newName: "Loans",
                newSchema: "artemisBankingPro");

            migrationBuilder.RenameTable(
                name: "LoanInstallments",
                schema: "artemisBank",
                newName: "LoanInstallments",
                newSchema: "artemisBankingPro");

            migrationBuilder.RenameTable(
                name: "IdempotencyRecords",
                schema: "artemisBank",
                newName: "IdempotencyRecords",
                newSchema: "artemisBankingPro");

            migrationBuilder.RenameTable(
                name: "CreditCards",
                schema: "artemisBank",
                newName: "CreditCards",
                newSchema: "artemisBankingPro");

            migrationBuilder.RenameTable(
                name: "CreditCardConsumptions",
                schema: "artemisBank",
                newName: "CreditCardConsumptions",
                newSchema: "artemisBankingPro");

            migrationBuilder.RenameTable(
                name: "Commerces",
                schema: "artemisBank",
                newName: "Commerces",
                newSchema: "artemisBankingPro");

            migrationBuilder.RenameTable(
                name: "Beneficiaries",
                schema: "artemisBank",
                newName: "Beneficiaries",
                newSchema: "artemisBankingPro");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "artemisBank");

            migrationBuilder.RenameTable(
                name: "Transactions",
                schema: "artemisBankingPro",
                newName: "Transactions",
                newSchema: "artemisBank");

            migrationBuilder.RenameTable(
                name: "SavingsAccounts",
                schema: "artemisBankingPro",
                newName: "SavingsAccounts",
                newSchema: "artemisBank");

            migrationBuilder.RenameTable(
                name: "Loans",
                schema: "artemisBankingPro",
                newName: "Loans",
                newSchema: "artemisBank");

            migrationBuilder.RenameTable(
                name: "LoanInstallments",
                schema: "artemisBankingPro",
                newName: "LoanInstallments",
                newSchema: "artemisBank");

            migrationBuilder.RenameTable(
                name: "IdempotencyRecords",
                schema: "artemisBankingPro",
                newName: "IdempotencyRecords",
                newSchema: "artemisBank");

            migrationBuilder.RenameTable(
                name: "CreditCards",
                schema: "artemisBankingPro",
                newName: "CreditCards",
                newSchema: "artemisBank");

            migrationBuilder.RenameTable(
                name: "CreditCardConsumptions",
                schema: "artemisBankingPro",
                newName: "CreditCardConsumptions",
                newSchema: "artemisBank");

            migrationBuilder.RenameTable(
                name: "Commerces",
                schema: "artemisBankingPro",
                newName: "Commerces",
                newSchema: "artemisBank");

            migrationBuilder.RenameTable(
                name: "Beneficiaries",
                schema: "artemisBankingPro",
                newName: "Beneficiaries",
                newSchema: "artemisBank");
        }
    }
}
