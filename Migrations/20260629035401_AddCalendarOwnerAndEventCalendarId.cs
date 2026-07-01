using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenderBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarOwnerAndEventCalendarId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CalendarId",
                table: "Events",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Calendar",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsPersonal",
                table: "Calendar",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Calendar",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CalendarId",
                table: "Events",
                column: "CalendarId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Calendar_CalendarId",
                table: "Events",
                column: "CalendarId",
                principalTable: "Calendar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Calendar_CalendarId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_CalendarId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CalendarId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Calendar");

            migrationBuilder.DropColumn(
                name: "IsPersonal",
                table: "Calendar");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Calendar");
        }
    }
}
