using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Taxi.Migrations
{
    public partial class images : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "ImagesIds",
                table: "DriverLicenses",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagesIds",
                table: "DriverLicenses");
        }
    }
}
