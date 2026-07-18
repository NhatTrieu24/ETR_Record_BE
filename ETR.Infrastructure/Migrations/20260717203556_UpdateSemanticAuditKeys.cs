using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSemanticAuditKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "UserProfiles",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "UserProfiles",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "SubjectSignoffs",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "SubjectSignoffs",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "Subjects",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Subjects",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "SubjectResults",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "EvaluatedBy",
                table: "SubjectResults",
                newName: "EvaluatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "SubjectResults",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "Sessions",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Sessions",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "Roles",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Roles",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "RetakeHistories",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "RetakeHistories",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "AuthorizedBy",
                table: "RetakeHistories",
                newName: "AuthorizedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "PracticalChecklists",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "PracticalChecklists",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "PracticalChecklistResults",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "PracticalChecklistResults",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "LearnerTypes",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "LearnerTypes",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "ExportJobs",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "ExportJobs",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "EvidenceTypes",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "EvidenceTypes",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "EvidenceFiles",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "EvidenceFiles",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "ETRCourseRecords",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "ETRCourseRecords",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "Departments",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Departments",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "DashboardSnapshots",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "DashboardSnapshots",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "CourseSubjects",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "CourseSubjects",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "Courses",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Courses",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "CourseEnrollments",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "CourseEnrollments",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "CompletionRequirements",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "CompletionRequirements",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "ClassStudents",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "ClassStudents",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "Classes",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Classes",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "AttendanceRecords",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "RecordedBy",
                table: "AttendanceRecords",
                newName: "RecordedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "AttendanceRecords",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "Assessments",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Assessments",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "AssessmentResults",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "AssessmentResults",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "ApprovalRequests",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "SubmittedBy",
                table: "ApprovalRequests",
                newName: "SubmittedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "ApprovalRequests",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "ApprovalHistories",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "ApprovalHistories",
                newName: "CreatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "Accounts",
                newName: "UpdatedByAccountId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Accounts",
                newName: "CreatedByAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "UserProfiles",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "UserProfiles",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "SubjectSignoffs",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "SubjectSignoffs",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "Subjects",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "Subjects",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "SubjectResults",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "EvaluatedByAccountId",
                table: "SubjectResults",
                newName: "EvaluatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "SubjectResults",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "Sessions",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "Sessions",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "Roles",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "Roles",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "RetakeHistories",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "RetakeHistories",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "AuthorizedByAccountId",
                table: "RetakeHistories",
                newName: "AuthorizedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "PracticalChecklists",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "PracticalChecklists",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "PracticalChecklistResults",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "PracticalChecklistResults",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "LearnerTypes",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "LearnerTypes",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "ExportJobs",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "ExportJobs",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "EvidenceTypes",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "EvidenceTypes",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "EvidenceFiles",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "EvidenceFiles",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "ETRCourseRecords",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "ETRCourseRecords",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "Departments",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "Departments",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "DashboardSnapshots",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "DashboardSnapshots",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "CourseSubjects",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "CourseSubjects",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "Courses",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "Courses",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "CourseEnrollments",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "CourseEnrollments",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "CompletionRequirements",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "CompletionRequirements",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "ClassStudents",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "ClassStudents",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "Classes",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "Classes",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "AttendanceRecords",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "RecordedByAccountId",
                table: "AttendanceRecords",
                newName: "RecordedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "AttendanceRecords",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "Assessments",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "Assessments",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "AssessmentResults",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "AssessmentResults",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "ApprovalRequests",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "SubmittedByAccountId",
                table: "ApprovalRequests",
                newName: "SubmittedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "ApprovalRequests",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "ApprovalHistories",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "ApprovalHistories",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedByAccountId",
                table: "Accounts",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByAccountId",
                table: "Accounts",
                newName: "CreatedBy");
        }
    }
}
