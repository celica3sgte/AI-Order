using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIOrder.Management.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    AspNetUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    MainImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Image1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Image2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Image3 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IngredientsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllergensJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifierGroupsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_AspNetUserId",
                table: "MenuItems",
                column: "AspNetUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuItems");
        }
    }
}
