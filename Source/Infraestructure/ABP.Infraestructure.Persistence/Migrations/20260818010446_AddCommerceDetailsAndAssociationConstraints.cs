using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABP.Infraestructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommerceDetailsAndAssociationConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "artemisBank",
                table: "Commerces",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Rnc",
                schema: "artemisBank",
                table: "Commerces",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: "");

            // Backfill legacy rows before creating the unique indexes. This keeps the
            // migration valid when a database already contains commerce records.
            migrationBuilder.Sql("""
                UPDATE "artemisBank"."Commerces"
                SET "Rnc" = CASE WHEN "Rnc" = '' THEN LPAD("Id"::text, 9, '0') ELSE "Rnc" END,
                    "Email" = CASE WHEN "Email" = '' THEN 'commerce-' || "Id"::text || '@artemisbanking.local' ELSE "Email" END
                WHERE "Rnc" = '' OR "Email" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Commerces_Email",
                schema: "artemisBank",
                table: "Commerces",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commerces_Rnc",
                schema: "artemisBank",
                table: "Commerces",
                column: "Rnc",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Commerces_Email",
                schema: "artemisBank",
                table: "Commerces");

            migrationBuilder.DropIndex(
                name: "IX_Commerces_Rnc",
                schema: "artemisBank",
                table: "Commerces");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "artemisBank",
                table: "Commerces");

            migrationBuilder.DropColumn(
                name: "Rnc",
                schema: "artemisBank",
                table: "Commerces");
        }
    }
}
