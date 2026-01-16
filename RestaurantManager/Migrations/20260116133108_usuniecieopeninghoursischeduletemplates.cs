using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManager.Migrations
{
    /// <inheritdoc />
    public partial class usuniecieopeninghoursischeduletemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateShiftSlots");

            migrationBuilder.DropTable(
                name: "ScheduleTemplates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplateShiftSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionTagId = table.Column<int>(type: "int", nullable: false),
                    ScheduleTemplateId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    RequiredEmployeeCount = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateShiftSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateShiftSlots_PositionTags_PositionTagId",
                        column: x => x.PositionTagId,
                        principalTable: "PositionTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateShiftSlots_ScheduleTemplates_ScheduleTemplateId",
                        column: x => x.ScheduleTemplateId,
                        principalTable: "ScheduleTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateShiftSlots_PositionTagId",
                table: "TemplateShiftSlots",
                column: "PositionTagId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateShiftSlots_ScheduleTemplateId",
                table: "TemplateShiftSlots",
                column: "ScheduleTemplateId");
        }
    }
}
