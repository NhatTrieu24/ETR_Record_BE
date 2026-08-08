using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClassStudentPointToEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_ClassStudents_ClassStudentId",
                table: "AttendanceRecords");

            // DATA BACKFILL — must run BEFORE dropping ClassStudents / renaming the column below.
            // AttendanceRecords.ClassStudentId currently holds ClassStudent.ClassStudentId values;
            // after this migration the same (renamed) column must hold
            // CourseEnrollment.EnrollmentId values instead. Without this UPDATE, the rename below
            // would silently keep the OLD numeric values under the NEW column name — every existing
            // AttendanceRecord would then point at the wrong (or a non-existent) Enrollment.
            migrationBuilder.Sql(@"
                UPDATE ar
                SET ar.ClassStudentId = cs.CourseEnrollmentId
                FROM AttendanceRecords ar
                INNER JOIN ClassStudents cs ON cs.ClassStudentId = ar.ClassStudentId;
            ");

            migrationBuilder.DropTable(
                name: "ClassStudents");

            migrationBuilder.RenameColumn(
                name: "ClassStudentId",
                table: "AttendanceRecords",
                newName: "EnrollmentId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecords_SessionId_ClassStudentId",
                table: "AttendanceRecords",
                newName: "IX_AttendanceRecords_SessionId_EnrollmentId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecords_ClassStudentId",
                table: "AttendanceRecords",
                newName: "IX_AttendanceRecords_EnrollmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_CourseEnrollments_EnrollmentId",
                table: "AttendanceRecords",
                column: "EnrollmentId",
                principalTable: "CourseEnrollments",
                principalColumn: "EnrollmentId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        // NOTE: this Down() recreates the ClassStudents TABLE STRUCTURE only — it does not (and
        // cannot) reconstruct the original ClassStudentId values or repopulate rows, since that data
        // was intentionally collapsed into CourseEnrollment by the Up() backfill above. Rolling back
        // this migration on a database with real data will leave ClassStudents empty and
        // AttendanceRecords.ClassStudentId holding EnrollmentId values, not the old ClassStudentId
        // values. Treat this Down() as "restore schema shape for a fresh/empty dev DB" only.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_CourseEnrollments_EnrollmentId",
                table: "AttendanceRecords");

            migrationBuilder.RenameColumn(
                name: "EnrollmentId",
                table: "AttendanceRecords",
                newName: "ClassStudentId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecords_SessionId_EnrollmentId",
                table: "AttendanceRecords",
                newName: "IX_AttendanceRecords_SessionId_ClassStudentId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecords_EnrollmentId",
                table: "AttendanceRecords",
                newName: "IX_AttendanceRecords_ClassStudentId");

            migrationBuilder.CreateTable(
                name: "ClassStudents",
                columns: table => new
                {
                    ClassStudentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    CourseEnrollmentId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByAccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassStudents", x => x.ClassStudentId);
                    table.ForeignKey(
                        name: "FK_ClassStudents_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassStudents_CourseEnrollments_CourseEnrollmentId",
                        column: x => x.CourseEnrollmentId,
                        principalTable: "CourseEnrollments",
                        principalColumn: "EnrollmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassStudents_AccountId",
                table: "ClassStudents",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassStudents_CourseEnrollmentId",
                table: "ClassStudents",
                column: "CourseEnrollmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_ClassStudents_ClassStudentId",
                table: "AttendanceRecords",
                column: "ClassStudentId",
                principalTable: "ClassStudents",
                principalColumn: "ClassStudentId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
