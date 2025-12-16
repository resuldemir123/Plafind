using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plafind.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessFeaturesJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeaturesJson",
                table: "Businesses",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeaturesJson",
                table: "Businesses");
        }
    }
}
