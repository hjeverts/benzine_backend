using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vehictory.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVehiclePhotoThumbnail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "FotoThumbnail",
                table: "Vehicles",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoThumbnail",
                table: "Vehicles");
        }
    }
}
