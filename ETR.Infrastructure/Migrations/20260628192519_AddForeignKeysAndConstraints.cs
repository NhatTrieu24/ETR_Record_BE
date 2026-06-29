using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeysAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ClassCode",
                table: "TrainingClasses",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "RoleName",
                table: "Roles",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TypeName",
                table: "LearnerTypes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "LearnerCode",
                table: "Learners",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "IdentificationNumber",
                table: "Learners",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Learners",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TypeName",
                table: "EvidenceTypes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DepartmentName",
                table: "Departments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AverageAttendanceRate",
                table: "DashboardSnapshots",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AverageAssessmentScore",
                table: "DashboardSnapshots",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "CourseCode",
                table: "Courses",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "AssessmentResults",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "AssessmentComponents",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PassingScore",
                table: "AssessmentComponents",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingClasses_ClassCode",
                table: "TrainingClasses",
                column: "ClassCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingClasses_CourseId",
                table: "TrainingClasses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearnerTypes_TypeName",
                table: "LearnerTypes",
                column: "TypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Learners_Email",
                table: "Learners",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Learners_IdentificationNumber",
                table: "Learners",
                column: "IdentificationNumber",
                unique: true,
                filter: "[IdentificationNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Learners_LearnerCode",
                table: "Learners",
                column: "LearnerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Learners_LearnerTypeId",
                table: "Learners",
                column: "LearnerTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceTypes_TypeName",
                table: "EvidenceTypes",
                column: "TypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceFiles_AssessmentResultId",
                table: "EvidenceFiles",
                column: "AssessmentResultId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceFiles_AttendanceRecordId",
                table: "EvidenceFiles",
                column: "AttendanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceFiles_ETRRecordId",
                table: "EvidenceFiles",
                column: "ETRRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceFiles_EvidenceTypeId",
                table: "EvidenceFiles",
                column: "EvidenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceFiles_LearnerId",
                table: "EvidenceFiles",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceFiles_UploadedBy",
                table: "EvidenceFiles",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ETRRecords_EnrollmentId",
                table: "ETRRecords",
                column: "EnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ETRChecklistProgresses_ChecklistItemId",
                table: "ETRChecklistProgresses",
                column: "ChecklistItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ETRChecklistProgresses_ETRRecordId_ChecklistItemId",
                table: "ETRChecklistProgresses",
                columns: new[] { "ETRRecordId", "ChecklistItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ETRChecklistProgresses_VerifiedBy",
                table: "ETRChecklistProgresses",
                column: "VerifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ClassId",
                table: "Enrollments",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CreatedBy",
                table: "Enrollments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_LearnerId_ClassId",
                table: "Enrollments",
                columns: new[] { "LearnerId", "ClassId" },
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
                name: "IX_ClassInstructors_ClassId_UserId",
                table: "ClassInstructors",
                columns: new[] { "ClassId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassInstructors_UserId",
                table: "ClassInstructors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_ClassId",
                table: "AttendanceSessions",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_ConfirmedBy",
                table: "AttendanceSessions",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_CreatedBy",
                table: "AttendanceSessions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_AttendanceSessionId_LearnerId",
                table: "AttendanceRecords",
                columns: new[] { "AttendanceSessionId", "LearnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ETRRecordId",
                table: "AttendanceRecords",
                column: "ETRRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_LearnerId",
                table: "AttendanceRecords",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_RecordedBy",
                table: "AttendanceRecords",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_AssessmentComponentId_LearnerId",
                table: "AssessmentResults",
                columns: new[] { "AssessmentComponentId", "LearnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_ETRRecordId",
                table: "AssessmentResults",
                column: "ETRRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_LearnerId",
                table: "AssessmentResults",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_RecordedBy",
                table: "AssessmentResults",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentComponents_CourseId",
                table: "AssessmentComponents",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentComponents_Courses_CourseId",
                table: "AssessmentComponents",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentResults_AssessmentComponents_AssessmentComponentId",
                table: "AssessmentResults",
                column: "AssessmentComponentId",
                principalTable: "AssessmentComponents",
                principalColumn: "AssessmentComponentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentResults_ETRRecords_ETRRecordId",
                table: "AssessmentResults",
                column: "ETRRecordId",
                principalTable: "ETRRecords",
                principalColumn: "ETRRecordId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentResults_Learners_LearnerId",
                table: "AssessmentResults",
                column: "LearnerId",
                principalTable: "Learners",
                principalColumn: "LearnerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentResults_Users_RecordedBy",
                table: "AssessmentResults",
                column: "RecordedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_AttendanceSessions_AttendanceSessionId",
                table: "AttendanceRecords",
                column: "AttendanceSessionId",
                principalTable: "AttendanceSessions",
                principalColumn: "AttendanceSessionId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_ETRRecords_ETRRecordId",
                table: "AttendanceRecords",
                column: "ETRRecordId",
                principalTable: "ETRRecords",
                principalColumn: "ETRRecordId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Learners_LearnerId",
                table: "AttendanceRecords",
                column: "LearnerId",
                principalTable: "Learners",
                principalColumn: "LearnerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Users_RecordedBy",
                table: "AttendanceRecords",
                column: "RecordedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSessions_TrainingClasses_ClassId",
                table: "AttendanceSessions",
                column: "ClassId",
                principalTable: "TrainingClasses",
                principalColumn: "ClassId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSessions_Users_ConfirmedBy",
                table: "AttendanceSessions",
                column: "ConfirmedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSessions_Users_CreatedBy",
                table: "AttendanceSessions",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassInstructors_TrainingClasses_ClassId",
                table: "ClassInstructors",
                column: "ClassId",
                principalTable: "TrainingClasses",
                principalColumn: "ClassId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassInstructors_Users_UserId",
                table: "ClassInstructors",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Learners_LearnerId",
                table: "Enrollments",
                column: "LearnerId",
                principalTable: "Learners",
                principalColumn: "LearnerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_TrainingClasses_ClassId",
                table: "Enrollments",
                column: "ClassId",
                principalTable: "TrainingClasses",
                principalColumn: "ClassId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Users_CreatedBy",
                table: "Enrollments",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ETRChecklistProgresses_ETRChecklistItems_ChecklistItemId",
                table: "ETRChecklistProgresses",
                column: "ChecklistItemId",
                principalTable: "ETRChecklistItems",
                principalColumn: "ChecklistItemId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ETRChecklistProgresses_ETRRecords_ETRRecordId",
                table: "ETRChecklistProgresses",
                column: "ETRRecordId",
                principalTable: "ETRRecords",
                principalColumn: "ETRRecordId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ETRChecklistProgresses_Users_VerifiedBy",
                table: "ETRChecklistProgresses",
                column: "VerifiedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ETRRecords_Enrollments_EnrollmentId",
                table: "ETRRecords",
                column: "EnrollmentId",
                principalTable: "Enrollments",
                principalColumn: "EnrollmentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceFiles_AssessmentResults_AssessmentResultId",
                table: "EvidenceFiles",
                column: "AssessmentResultId",
                principalTable: "AssessmentResults",
                principalColumn: "AssessmentResultId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceFiles_AttendanceRecords_AttendanceRecordId",
                table: "EvidenceFiles",
                column: "AttendanceRecordId",
                principalTable: "AttendanceRecords",
                principalColumn: "AttendanceRecordId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceFiles_ETRRecords_ETRRecordId",
                table: "EvidenceFiles",
                column: "ETRRecordId",
                principalTable: "ETRRecords",
                principalColumn: "ETRRecordId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceFiles_EvidenceTypes_EvidenceTypeId",
                table: "EvidenceFiles",
                column: "EvidenceTypeId",
                principalTable: "EvidenceTypes",
                principalColumn: "EvidenceTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceFiles_Learners_LearnerId",
                table: "EvidenceFiles",
                column: "LearnerId",
                principalTable: "Learners",
                principalColumn: "LearnerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceFiles_Users_UploadedBy",
                table: "EvidenceFiles",
                column: "UploadedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Learners_LearnerTypes_LearnerTypeId",
                table: "Learners",
                column: "LearnerTypeId",
                principalTable: "LearnerTypes",
                principalColumn: "LearnerTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingClasses_Courses_CourseId",
                table: "TrainingClasses",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "CourseId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "DepartmentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "RoleId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentComponents_Courses_CourseId",
                table: "AssessmentComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentResults_AssessmentComponents_AssessmentComponentId",
                table: "AssessmentResults");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentResults_ETRRecords_ETRRecordId",
                table: "AssessmentResults");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentResults_Learners_LearnerId",
                table: "AssessmentResults");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentResults_Users_RecordedBy",
                table: "AssessmentResults");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_AttendanceSessions_AttendanceSessionId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_ETRRecords_ETRRecordId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Learners_LearnerId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Users_RecordedBy",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSessions_TrainingClasses_ClassId",
                table: "AttendanceSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSessions_Users_ConfirmedBy",
                table: "AttendanceSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSessions_Users_CreatedBy",
                table: "AttendanceSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassInstructors_TrainingClasses_ClassId",
                table: "ClassInstructors");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassInstructors_Users_UserId",
                table: "ClassInstructors");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Learners_LearnerId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_TrainingClasses_ClassId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Users_CreatedBy",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_ETRChecklistProgresses_ETRChecklistItems_ChecklistItemId",
                table: "ETRChecklistProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_ETRChecklistProgresses_ETRRecords_ETRRecordId",
                table: "ETRChecklistProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_ETRChecklistProgresses_Users_VerifiedBy",
                table: "ETRChecklistProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_ETRRecords_Enrollments_EnrollmentId",
                table: "ETRRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceFiles_AssessmentResults_AssessmentResultId",
                table: "EvidenceFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceFiles_AttendanceRecords_AttendanceRecordId",
                table: "EvidenceFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceFiles_ETRRecords_ETRRecordId",
                table: "EvidenceFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceFiles_EvidenceTypes_EvidenceTypeId",
                table: "EvidenceFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceFiles_Learners_LearnerId",
                table: "EvidenceFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceFiles_Users_UploadedBy",
                table: "EvidenceFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Learners_LearnerTypes_LearnerTypeId",
                table: "Learners");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingClasses_Courses_CourseId",
                table: "TrainingClasses");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DepartmentId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TrainingClasses_ClassCode",
                table: "TrainingClasses");

            migrationBuilder.DropIndex(
                name: "IX_TrainingClasses_CourseId",
                table: "TrainingClasses");

            migrationBuilder.DropIndex(
                name: "IX_Roles_RoleName",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_LearnerTypes_TypeName",
                table: "LearnerTypes");

            migrationBuilder.DropIndex(
                name: "IX_Learners_Email",
                table: "Learners");

            migrationBuilder.DropIndex(
                name: "IX_Learners_IdentificationNumber",
                table: "Learners");

            migrationBuilder.DropIndex(
                name: "IX_Learners_LearnerCode",
                table: "Learners");

            migrationBuilder.DropIndex(
                name: "IX_Learners_LearnerTypeId",
                table: "Learners");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceTypes_TypeName",
                table: "EvidenceTypes");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceFiles_AssessmentResultId",
                table: "EvidenceFiles");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceFiles_AttendanceRecordId",
                table: "EvidenceFiles");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceFiles_ETRRecordId",
                table: "EvidenceFiles");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceFiles_EvidenceTypeId",
                table: "EvidenceFiles");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceFiles_LearnerId",
                table: "EvidenceFiles");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceFiles_UploadedBy",
                table: "EvidenceFiles");

            migrationBuilder.DropIndex(
                name: "IX_ETRRecords_EnrollmentId",
                table: "ETRRecords");

            migrationBuilder.DropIndex(
                name: "IX_ETRChecklistProgresses_ChecklistItemId",
                table: "ETRChecklistProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ETRChecklistProgresses_ETRRecordId_ChecklistItemId",
                table: "ETRChecklistProgresses");

            migrationBuilder.DropIndex(
                name: "IX_ETRChecklistProgresses_VerifiedBy",
                table: "ETRChecklistProgresses");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_ClassId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_CreatedBy",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_LearnerId_ClassId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_DepartmentName",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseCode",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_ClassInstructors_ClassId_UserId",
                table: "ClassInstructors");

            migrationBuilder.DropIndex(
                name: "IX_ClassInstructors_UserId",
                table: "ClassInstructors");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceSessions_ClassId",
                table: "AttendanceSessions");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceSessions_ConfirmedBy",
                table: "AttendanceSessions");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceSessions_CreatedBy",
                table: "AttendanceSessions");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_AttendanceSessionId_LearnerId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_ETRRecordId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_LearnerId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_RecordedBy",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_AssessmentComponentId_LearnerId",
                table: "AssessmentResults");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_ETRRecordId",
                table: "AssessmentResults");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_LearnerId",
                table: "AssessmentResults");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_RecordedBy",
                table: "AssessmentResults");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentComponents_CourseId",
                table: "AssessmentComponents");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ClassCode",
                table: "TrainingClasses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "RoleName",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "TypeName",
                table: "LearnerTypes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LearnerCode",
                table: "Learners",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "IdentificationNumber",
                table: "Learners",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Learners",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TypeName",
                table: "EvidenceTypes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "DepartmentName",
                table: "Departments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AverageAttendanceRate",
                table: "DashboardSnapshots",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AverageAssessmentScore",
                table: "DashboardSnapshots",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<string>(
                name: "CourseCode",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score",
                table: "AssessmentResults",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "AssessmentComponents",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PassingScore",
                table: "AssessmentComponents",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");
        }
    }
}
