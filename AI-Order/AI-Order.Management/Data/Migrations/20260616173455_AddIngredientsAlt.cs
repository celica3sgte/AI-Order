using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIOrder.Management.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientsAlt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IngredientsJsonAlt",
                table: "MenuItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IngredientsJsonAlt",
                table: "MenuItems");
        }
    }
}
