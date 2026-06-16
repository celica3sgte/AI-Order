using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIOrder.Management.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemAltLanguageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionAlt",
                table: "MenuItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifierGroupsJsonAlt",
                table: "MenuItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "NameAlt",
                table: "MenuItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionAlt",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ModifierGroupsJsonAlt",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "NameAlt",
                table: "MenuItems");
        }
    }
}
