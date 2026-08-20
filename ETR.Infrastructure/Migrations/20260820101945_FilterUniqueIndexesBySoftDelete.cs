using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FilterUniqueIndexesBySoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_SubjectCode",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_SubjectResults_EtrId_CourseId_SubjectId",
                table: "SubjectResults");

            migrationBuilder.DropIndex(
                name: "IX_Roles_RoleName",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_PracticalChecklistResults_SubjectResultId_PracticalChecklistId",
                table: "PracticalChecklistResults");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceTypes_TypeName",
                table: "EvidenceTypes");

            migrationBuilder.DropIndex(
                name: "IX_ETRCourseRecords_EnrollmentId",
                table: "ETRCourseRecords");

            migrationBuilder.DropIndex(
                name: "IX_Departments_DepartmentName",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCode",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_AccountId_ClassId",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_ClassSubjects_ClassId_SubjectId",
                table: "ClassSubjects");

            migrationBuilder.DropIndex(
                name: "IX_Classes_ClassCode",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_SessionId_EnrollmentId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_AssessmentId_AccountId_SessionId",
                table: "AssessmentResults");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Username",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL AND [Email] <> '' AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SubjectCode",
                table: "Subjects",
                column: "SubjectCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectResults_EtrId_CourseId_SubjectId",
                table: "SubjectResults",
                columns: new[] { "EtrId", "CourseId", "SubjectId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PracticalChecklistResults_SubjectResultId_PracticalChecklistId",
                table: "PracticalChecklistResults",
                columns: new[] { "SubjectResultId", "PracticalChecklistId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceTypes_TypeName",
                table: "EvidenceTypes",
                column: "TypeName",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ETRCourseRecords_EnrollmentId",
                table: "ETRCourseRecords",
                column: "EnrollmentId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentName",
                table: "Departments",
                column: "DepartmentName",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCode",
                table: "Courses",
                column: "CourseCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_AccountId_ClassId",
                table: "CourseEnrollments",
                columns: new[] { "AccountId", "ClassId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSubjects_ClassId_SubjectId",
                table: "ClassSubjects",
                columns: new[] { "ClassId", "SubjectId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_ClassCode",
                table: "Classes",
                column: "ClassCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_SessionId_EnrollmentId",
                table: "AttendanceRecords",
                columns: new[] { "SessionId", "EnrollmentId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_AssessmentId_AccountId_SessionId_AttemptNo",
                table: "AssessmentResults",
                columns: new[] { "AssessmentId", "AccountId", "SessionId", "AttemptNo" },
                unique: true,
                filter: "[SessionId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Username",
                table: "Accounts",
                column: "Username",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_SubjectCode",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_SubjectResults_EtrId_CourseId_SubjectId",
                table: "SubjectResults");

            migrationBuilder.DropIndex(
                name: "IX_Roles_RoleName",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_PracticalChecklistResults_SubjectResultId_PracticalChecklistId",
                table: "PracticalChecklistResults");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceTypes_TypeName",
                table: "EvidenceTypes");

            migrationBuilder.DropIndex(
                name: "IX_ETRCourseRecords_EnrollmentId",
                table: "ETRCourseRecords");

            migrationBuilder.DropIndex(
                name: "IX_Departments_DepartmentName",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCode",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_AccountId_ClassId",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_ClassSubjects_ClassId_SubjectId",
                table: "ClassSubjects");

            migrationBuilder.DropIndex(
                name: "IX_Classes_ClassCode",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_SessionId_EnrollmentId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_AssessmentId_AccountId_SessionId_AttemptNo",
                table: "AssessmentResults");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Username",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL AND [Email] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SubjectCode",
                table: "Subjects",
                column: "SubjectCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectResults_EtrId_CourseId_SubjectId",
                table: "SubjectResults",
                columns: new[] { "EtrId", "CourseId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticalChecklistResults_SubjectResultId_PracticalChecklistId",
                table: "PracticalChecklistResults",
                columns: new[] { "SubjectResultId", "PracticalChecklistId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceTypes_TypeName",
                table: "EvidenceTypes",
                column: "TypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ETRCourseRecords_EnrollmentId",
                table: "ETRCourseRecords",
                column: "EnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentName",
                table: "Departments",
                column: "DepartmentName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCode",
                table: "Courses",
                column: "CourseCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_AccountId_ClassId",
                table: "CourseEnrollments",
                columns: new[] { "AccountId", "ClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSubjects_ClassId_SubjectId",
                table: "ClassSubjects",
                columns: new[] { "ClassId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_ClassCode",
                table: "Classes",
                column: "ClassCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_SessionId_EnrollmentId",
                table: "AttendanceRecords",
                columns: new[] { "SessionId", "EnrollmentId" },
                unique: true);

            // Restores the real pre-migration index (3 columns, no AttemptNo) — the DB was never
            // actually migrated to the 4-column version the model briefly declared without a migration.
            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_AssessmentId_AccountId_SessionId",
                table: "AssessmentResults",
                columns: new[] { "AssessmentId", "AccountId", "SessionId" },
                unique: true,
                filter: "[SessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Username",
                table: "Accounts",
                column: "Username",
                unique: true);
        }
    }
}
