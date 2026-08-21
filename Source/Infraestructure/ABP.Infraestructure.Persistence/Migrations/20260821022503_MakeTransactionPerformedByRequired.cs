using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABP.Infraestructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeTransactionPerformedByRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"artemisBankingPro\".\"Transactions\" " +
                "SET \"PerformedByUserId\" = 'legacy-system' " +
                "WHERE \"PerformedByUserId\" IS NULL OR BTRIM(\"PerformedByUserId\") = '';");

            migrationBuilder.AlterColumn<string>(
                name: "PerformedByUserId",
                schema: "artemisBankingPro",
                table: "Transactions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PerformedByUserId",
                schema: "artemisBankingPro",
                table: "Transactions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);
        }
    }
}
