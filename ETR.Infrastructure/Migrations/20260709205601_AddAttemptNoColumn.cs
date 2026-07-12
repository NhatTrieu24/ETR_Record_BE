using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptNoColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptNo",
                table: "RetakeHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttemptNo",
                table: "AssessmentResults",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptNo",
                table: "RetakeHistories");

            migrationBuilder.DropColumn(
                name: "AttemptNo",
                table: "AssessmentResults");
        }
    }
}
