using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenderBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountsIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountsIds",
                table: "Events",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountsIds",
                table: "Events");
        }
    }
}
