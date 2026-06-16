using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIOrder.Management.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModifierGroupDisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "ModifierGroups",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayNameAlt",
                table: "ModifierGroups",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "ModifierGroups");

            migrationBuilder.DropColumn(
                name: "DisplayNameAlt",
                table: "ModifierGroups");
        }
    }
}
