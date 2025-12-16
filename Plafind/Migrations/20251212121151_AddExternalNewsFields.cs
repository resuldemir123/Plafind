using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plafind.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalNewsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalSource",
                table: "News",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsExternal",
                table: "News",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "News",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalSource",
                table: "News");

            migrationBuilder.DropColumn(
                name: "IsExternal",
                table: "News");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "News");
        }
    }
}
