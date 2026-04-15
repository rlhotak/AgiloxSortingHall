using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgiloxSortingHall.Migrations
{
    /// <inheritdoc />
    public partial class SplitStationAreaNameIntoPickupAndDrop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StationAreaName",
                table: "HallSettings",
                newName: "PickupStationAreaName");

            migrationBuilder.AddColumn<string>(
                name: "DropStationAreaName",
                table: "HallSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "HallSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DropStationAreaName", "PickupStationAreaName" },
                values: new object[] { "Hotovo", "Buffer" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropStationAreaName",
                table: "HallSettings");

            migrationBuilder.RenameColumn(
                name: "PickupStationAreaName",
                table: "HallSettings",
                newName: "StationAreaName");

            migrationBuilder.UpdateData(
                table: "HallSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "StationAreaName",
                value: "Hotovo");
        }
    }
}
