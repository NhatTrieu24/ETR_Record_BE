using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionIdColumnToAssessmentResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentResults_Sessions_SessionId1",
                table: "AssessmentResults");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_SessionId1",
                table: "AssessmentResults");

            migrationBuilder.DropColumn(
                name: "SessionId1",
                table: "AssessmentResults");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SessionId1",
                table: "AssessmentResults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_SessionId1",
                table: "AssessmentResults",
                column: "SessionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentResults_Sessions_SessionId1",
                table: "AssessmentResults",
                column: "SessionId1",
                principalTable: "Sessions",
                principalColumn: "SessionId");
        }
    }
}
