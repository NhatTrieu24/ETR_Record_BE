using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileStatusAndRemoveDashboardSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardSnapshots");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "UserProfiles");

            migrationBuilder.CreateTable(
                name: "DashboardSnapshots",
                columns: table => new
                {
                    SnapshotId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AverageAssessmentScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AverageAttendanceRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CompletedETRs = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    MissingEvidenceETRs = table.Column<int>(type: "int", nullable: false),
                    PendingETRs = table.Column<int>(type: "int", nullable: false),
                    RejectedETRs = table.Column<int>(type: "int", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalClasses = table.Column<int>(type: "int", nullable: false),
                    TotalETRs = table.Column<int>(type: "int", nullable: false),
                    TotalLearners = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByAccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardSnapshots", x => x.SnapshotId);
                });
        }
    }
}
