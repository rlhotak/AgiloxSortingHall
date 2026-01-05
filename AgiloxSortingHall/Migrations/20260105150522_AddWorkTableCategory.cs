using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgiloxSortingHall.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkTableCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "WorkTables",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "WorkTables");
        }
    }
}
