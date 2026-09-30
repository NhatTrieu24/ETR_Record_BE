using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseVersioningAndEvaluationSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCode",
                table: "Courses");

            migrationBuilder.AddColumn<bool>(
                name: "IsMandatorySnapshot",
                table: "SubjectResults",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredHoursSnapshot",
                table: "SubjectResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredSessionsSnapshot",
                table: "SubjectResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SequenceNoSnapshot",
                table: "SubjectResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectCodeSnapshot",
                table: "SubjectResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectNameSnapshot",
                table: "SubjectResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectTypeSnapshot",
                table: "SubjectResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectVersionSnapshot",
                table: "SubjectResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMandatorySnapshot",
                table: "PracticalChecklistResults",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousVersionId",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CourseVersionNo",
                table: "Classes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("UPDATE Classes SET CourseVersionNo = 1 WHERE CourseVersionNo = 0 OR CourseVersionNo IS NULL;");

            migrationBuilder.AddColumn<bool>(
                name: "IsMandatorySnapshot",
                table: "AssessmentResults",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCode_VersionNo",
                table: "Courses",
                columns: new[] { "CourseCode", "VersionNo" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCode_VersionNo",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IsMandatorySnapshot",
                table: "SubjectResults");

            migrationBuilder.DropColumn(
                name: "RequiredHoursSnapshot",
                table: "SubjectResults");

            migrationBuilder.DropColumn(
                name: "RequiredSessionsSnapshot",
                table: "SubjectResults");

            migrationBuilder.DropColumn(
                name: "SequenceNoSnapshot",
                table: "SubjectResults");

            migrationBuilder.DropColumn(
                name: "SubjectCodeSnapshot",
                table: "SubjectResults");

            migrationBuilder.DropColumn(
                name: "SubjectNameSnapshot",
                table: "SubjectResults");

            migrationBuilder.DropColumn(
                name: "SubjectTypeSnapshot",
                table: "SubjectResults");

            migrationBuilder.DropColumn(
                name: "SubjectVersionSnapshot",
                table: "SubjectResults");

            migrationBuilder.DropColumn(
                name: "IsMandatorySnapshot",
                table: "PracticalChecklistResults");

            migrationBuilder.DropColumn(
                name: "PreviousVersionId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CourseVersionNo",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "IsMandatorySnapshot",
                table: "AssessmentResults");

            // Rollback Guard: Check if multiple active course versions with the same CourseCode exist.
            // If duplicate CourseCodes exist (where IsDeleted = 0), recreating the unique index IX_Courses_CourseCode will fail.
            // Rather than silently deleting user data, we halt rollback with an informative error message.
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT CourseCode 
                    FROM Courses 
                    WHERE [IsDeleted] = 0 
                    GROUP BY CourseCode 
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51000, 'Cannot rollback migration: multiple Course records with the same CourseCode exist in the database (filter [IsDeleted] = 0). Please ensure only a single record per CourseCode remains not deleted (soft-delete or resolve duplicate Course versions) before rolling back the course versioning schema.', 1;
                END
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCode",
                table: "Courses",
                column: "CourseCode",
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
