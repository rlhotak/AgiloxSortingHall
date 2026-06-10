using AgiloxSortingHall.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgiloxSortingHall.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260605080000_AddPickedSlotToRowCall")]
    public partial class AddPickedSlotToRowCall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PickedSlotId",
                table: "RowCalls",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RowCalls_PickedSlotId",
                table: "RowCalls",
                column: "PickedSlotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RowCalls_PickedSlotId",
                table: "RowCalls");

            migrationBuilder.DropColumn(
                name: "PickedSlotId",
                table: "RowCalls");
        }
    }
}
