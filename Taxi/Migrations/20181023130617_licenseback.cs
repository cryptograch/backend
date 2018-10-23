using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Taxi.Migrations
{
    public partial class licenseback : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagesIds",
                table: "DriverLicenses");

            migrationBuilder.AddColumn<string>(
                name: "BackId",
                table: "DriverLicenses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrontId",
                table: "DriverLicenses",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackId",
                table: "DriverLicenses");

            migrationBuilder.DropColumn(
                name: "FrontId",
                table: "DriverLicenses");

            migrationBuilder.AddColumn<List<string>>(
                name: "ImagesIds",
                table: "DriverLicenses",
                nullable: true);
        }
    }
}
