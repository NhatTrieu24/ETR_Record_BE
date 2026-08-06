using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticalChecklistIdToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PracticalChecklistId",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_PracticalChecklistId",
                table: "Sessions",
                column: "PracticalChecklistId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_PracticalChecklists_PracticalChecklistId",
                table: "Sessions",
                column: "PracticalChecklistId",
                principalTable: "PracticalChecklists",
                principalColumn: "PracticalChecklistId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_PracticalChecklists_PracticalChecklistId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_PracticalChecklistId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "PracticalChecklistId",
                table: "Sessions");
        }
    }
}
