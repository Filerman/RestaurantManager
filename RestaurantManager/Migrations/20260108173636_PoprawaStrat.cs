using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManager.Migrations
{
    /// <inheritdoc />
    public partial class PoprawaStrat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CustomItemCost",
                table: "LossLogs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomItemName",
                table: "LossLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MenuItemId",
                table: "LossLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "LossLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LossLogs_MenuItemId",
                table: "LossLogs",
                column: "MenuItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_LossLogs_MenuItems_MenuItemId",
                table: "LossLogs",
                column: "MenuItemId",
                principalTable: "MenuItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LossLogs_MenuItems_MenuItemId",
                table: "LossLogs");

            migrationBuilder.DropIndex(
                name: "IX_LossLogs_MenuItemId",
                table: "LossLogs");

            migrationBuilder.DropColumn(
                name: "CustomItemCost",
                table: "LossLogs");

            migrationBuilder.DropColumn(
                name: "CustomItemName",
                table: "LossLogs");

            migrationBuilder.DropColumn(
                name: "MenuItemId",
                table: "LossLogs");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "LossLogs");
        }
    }
}
