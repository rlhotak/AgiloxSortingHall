using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgiloxSortingHall.Migrations
{
    /// <inheritdoc />
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

            migrationBuilder.AddForeignKey(
                name: "FK_RowCalls_PalletSlots_PickedSlotId",
                table: "RowCalls",
                column: "PickedSlotId",
                principalTable: "PalletSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RowCalls_PalletSlots_PickedSlotId",
                table: "RowCalls");

            migrationBuilder.DropIndex(
                name: "IX_RowCalls_PickedSlotId",
                table: "RowCalls");

            migrationBuilder.DropColumn(
                name: "PickedSlotId",
                table: "RowCalls");
        }
    }
}
