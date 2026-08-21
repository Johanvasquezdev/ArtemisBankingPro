using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABP.Infraestructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommerceContactDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByAdminId",
                schema: "artemisBankingPro",
                table: "Commerces",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                schema: "artemisBankingPro",
                table: "Commerces",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByAdminId",
                schema: "artemisBankingPro",
                table: "Commerces");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                schema: "artemisBankingPro",
                table: "Commerces");
        }
    }
}
