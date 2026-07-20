using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreToPracticalChecklistResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "PracticalChecklistResults");

            migrationBuilder.AddColumn<string>(
                name: "ResultStatus",
                table: "PracticalChecklistResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                table: "PracticalChecklistResults",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResultStatus",
                table: "PracticalChecklistResults");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "PracticalChecklistResults");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "PracticalChecklistResults",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
