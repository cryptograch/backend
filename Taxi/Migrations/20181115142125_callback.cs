using Microsoft.EntityFrameworkCore.Migrations;

namespace Taxi.Migrations
{
    public partial class callback : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Callback",
                table: "Trips",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Callback",
                table: "Trips");
        }
    }
}
