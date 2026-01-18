using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManager.Migrations
{
    /// <inheritdoc />
    public partial class nowabazadanychiusunieciegooglemaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleMapsLink",
                table: "ContactInfos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleMapsLink",
                table: "ContactInfos",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
