using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeightSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WeightSnapshot",
                table: "AssessmentResults",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeightSnapshot",
                table: "AssessmentResults");
        }
    }
}
