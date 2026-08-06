using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentIdToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssessmentId",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_AssessmentId",
                table: "Sessions",
                column: "AssessmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Assessments_AssessmentId",
                table: "Sessions",
                column: "AssessmentId",
                principalTable: "Assessments",
                principalColumn: "AssessmentId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Assessments_AssessmentId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_AssessmentId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "AssessmentId",
                table: "Sessions");
        }
    }
}
