using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgiloxSortingHall.Migrations
{
    /// <inheritdoc />
    public partial class AddPutRowStrategy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DropRowSelectionStrategy",
                table: "HallSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "HallSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "DropRowSelectionStrategy",
                value: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropRowSelectionStrategy",
                table: "HallSettings");
        }
    }
}
