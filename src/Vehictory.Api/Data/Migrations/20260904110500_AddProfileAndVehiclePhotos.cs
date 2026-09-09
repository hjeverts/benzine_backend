using Vehictory.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vehictory.Api.Data.Migrations;

[DbContext(typeof(VehictoryDbContext))]
[Migration("20260904110500_AddProfileAndVehiclePhotos")]
public partial class AddProfileAndVehiclePhotos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "Avatar",
            table: "Users",
            type: "bytea",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AvatarContentType",
            table: "Users",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "Foto",
            table: "Vehicles",
            type: "bytea",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FotoContentType",
            table: "Vehicles",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Avatar", table: "Users");
        migrationBuilder.DropColumn(name: "AvatarContentType", table: "Users");
        migrationBuilder.DropColumn(name: "Foto", table: "Vehicles");
        migrationBuilder.DropColumn(name: "FotoContentType", table: "Vehicles");
    }
}
