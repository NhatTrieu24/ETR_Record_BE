using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionIdToAssessmentResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_AssessmentId_AccountId",
                table: "AssessmentResults");

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "AssessmentResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionId1",
                table: "AssessmentResults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_AssessmentId_AccountId_SessionId",
                table: "AssessmentResults",
                columns: new[] { "AssessmentId", "AccountId", "SessionId" },
                unique: true,
                filter: "[SessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_SessionId",
                table: "AssessmentResults",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_SessionId1",
                table: "AssessmentResults",
                column: "SessionId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentResults_Sessions_SessionId",
                table: "AssessmentResults",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentResults_Sessions_SessionId1",
                table: "AssessmentResults",
                column: "SessionId1",
                principalTable: "Sessions",
                principalColumn: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentResults_Sessions_SessionId",
                table: "AssessmentResults");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentResults_Sessions_SessionId1",
                table: "AssessmentResults");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_AssessmentId_AccountId_SessionId",
                table: "AssessmentResults");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_SessionId",
                table: "AssessmentResults");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_SessionId1",
                table: "AssessmentResults");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "AssessmentResults");

            migrationBuilder.DropColumn(
                name: "SessionId1",
                table: "AssessmentResults");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_AssessmentId_AccountId",
                table: "AssessmentResults",
                columns: new[] { "AssessmentId", "AccountId" },
                unique: true);
        }
    }
}
