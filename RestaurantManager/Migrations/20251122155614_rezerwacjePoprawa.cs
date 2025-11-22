using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManager.Migrations
{
    /// <inheritdoc />
    public partial class rezerwacjePoprawa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Tables_TableId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Reservations");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Reservations",
                newName: "CustomerPhone");

            migrationBuilder.RenameColumn(
                name: "PartySize",
                table: "Reservations",
                newName: "Status");

            migrationBuilder.AlterColumn<int>(
                name: "TableId",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Tables_TableId",
                table: "Reservations",
                column: "TableId",
                principalTable: "Tables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Tables_TableId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Reservations");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Reservations",
                newName: "PartySize");

            migrationBuilder.RenameColumn(
                name: "CustomerPhone",
                table: "Reservations",
                newName: "Phone");

            migrationBuilder.AlterColumn<int>(
                name: "TableId",
                table: "Reservations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "Reservations",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Tables_TableId",
                table: "Reservations",
                column: "TableId",
                principalTable: "Tables",
                principalColumn: "Id");
        }
    }
}
