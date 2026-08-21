using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABP.Infraestructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddCommerceUserAssociationConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the first active association and detach later duplicates before
            // enforcing the one-commerce-user rule at the database level.
            migrationBuilder.Sql("""
                WITH duplicate_commerce_users AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (PARTITION BY "CommerceId" ORDER BY "Id") AS row_number
                    FROM "identity"."Users"
                    WHERE "Role" = 'Commerce' AND "CommerceId" IS NOT NULL
                )
                UPDATE "identity"."Users" AS users
                SET "CommerceId" = NULL
                FROM duplicate_commerce_users AS duplicates
                WHERE users."Id" = duplicates."Id" AND duplicates.row_number > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CommerceId",
                schema: "identity",
                table: "Users",
                column: "CommerceId",
                unique: true,
                filter: "\"Role\" = 'Commerce' AND \"CommerceId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_CommerceId",
                schema: "identity",
                table: "Users");
        }
    }
}
