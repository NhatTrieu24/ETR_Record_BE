using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    public partial class RefactorIdentityAndETR : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ==============================================================================
            // BƯỚC 1: TẠO BẢNG MỚI TRƯỚC (Được đẩy lên trên cùng để hứng dữ liệu)
            // ==============================================================================
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_Accounts_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Accounts_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    UserCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Organization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LearnerTypeId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProfiles_LearnerTypes_LearnerTypeId",
                        column: x => x.LearnerTypeId,
                        principalTable: "LearnerTypes",
                        principalColumn: "LearnerTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassStudents",
                columns: table => new
                {
                    ClassStudentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseEnrollmentId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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

            // ==============================================================================
            // BƯỚC 2: SCRIPT SQL DI CƯ DỮ LIỆU & XỬ LÝ XUNG ĐỘT ID
            // ==============================================================================
            migrationBuilder.Sql(@"
                -- Bật chế độ cho phép chèn ID thủ công vào bảng Accounts
                SET IDENTITY_INSERT Accounts ON;

                -- Đảm bảo Role 'Learner' tồn tại
                DECLARE @LearnerRoleId INT = (SELECT TOP 1 RoleId FROM Roles WHERE RoleName = 'Learner');
                IF @LearnerRoleId IS NULL
                BEGIN
                    INSERT INTO Roles (RoleName, Description, CreatedAt, IsDeleted) VALUES ('Learner', 'Learner Role', GETDATE(), 0);
                    SET @LearnerRoleId = SCOPE_IDENTITY();
                END

                -- 1. Chuyển Users sang Accounts (Giữ nguyên ID cũ)
                INSERT INTO Accounts (AccountId, Username, PasswordHash, RoleId, DepartmentId, Status, CreatedAt, IsDeleted)
                SELECT UserId, Username, PasswordHash, RoleId, DepartmentId, CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END, CreatedAt, IsDeleted
                FROM Users;

                -- 2. Chuyển Learners sang Accounts (Cộng 1.000.000 vào ID để tránh dẫm đạp ID với Users)
                INSERT INTO Accounts (AccountId, Username, PasswordHash, RoleId, DepartmentId, Status, CreatedAt, IsDeleted)
                SELECT LearnerId + 1000000, Email, 'default_hash', @LearnerRoleId, (SELECT TOP 1 DepartmentId FROM Departments), Status, CreatedAt, IsDeleted
                FROM Learners
                WHERE Email IS NOT NULL; 

                SET IDENTITY_INSERT Accounts OFF;

                -- 3. Chuyển Users sang UserProfiles
                INSERT INTO UserProfiles (AccountId, UserCode, FullName, Email, Phone, DateOfBirth, Gender, Organization, LearnerTypeId, CreatedAt, IsDeleted)
                SELECT UserId, 'USR-' + CAST(UserId AS VARCHAR), FullName, Email, Phone, '1900-01-01', 'Unknown', 'Internal', NULL, GETDATE(), 0
                FROM Users;

                -- 4. Chuyển Learners sang UserProfiles (ID cũng được cộng thêm 1.000.000)
                INSERT INTO UserProfiles (AccountId, UserCode, FullName, Email, Phone, DateOfBirth, Gender, Organization, LearnerTypeId, CreatedAt, IsDeleted)
                SELECT LearnerId + 1000000, LearnerCode, FullName, Email, Phone, DateOfBirth, Gender, Organization, LearnerTypeId, GETDATE(), 0
                FROM Learners
                WHERE Email IS NOT NULL;
            ");

            // ==============================================================================
            // BƯỚC 3: DỌN DẸP BẢNG CŨ VÀ ĐỔI TÊN CỘT (Code gốc do EF Core sinh ra)
            // ==============================================================================
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentResults_Learners_LearnerId",
                table: "AssessmentResults");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_CourseEnrollments_EnrollmentId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Learners_LearnerId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_Learners_LearnerId",
                table: "CourseEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceFiles_Learners_LearnerId",
                table: "EvidenceFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SubjectResults_CourseEnrollments_EnrollmentId",
                table: "SubjectResults");

            migrationBuilder.DropTable(
                name: "Learners");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_EnrollmentId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "EnrollmentId",
                table: "AttendanceRecords");

            migrationBuilder.RenameColumn(
                name: "SignoffBy",
                table: "SubjectSignoffs",
                newName: "SignoffByAccountId");

            migrationBuilder.RenameColumn(
                name: "EnrollmentId",
                table: "SubjectResults",
                newName: "EtrId");

            migrationBuilder.RenameIndex(
                name: "IX_SubjectResults_EnrollmentId_CourseId_SubjectId",
                table: "SubjectResults",
                newName: "IX_SubjectResults_EtrId_CourseId_SubjectId");

            migrationBuilder.RenameColumn(
                name: "ConfirmedBy",
                table: "Sessions",
                newName: "ConfirmedByAccountId");

            migrationBuilder.RenameColumn(
                name: "VerifiedBy",
                table: "PracticalChecklistResults",
                newName: "VerifiedByAccountId");

            migrationBuilder.RenameColumn(
                name: "RequestedBy",
                table: "ExportJobs",
                newName: "RequestedByAccountId");

            migrationBuilder.RenameColumn(
                name: "VerifiedBy",
                table: "EvidenceFiles",
                newName: "VerifiedByAccountId");

            migrationBuilder.RenameColumn(
                name: "UploadedBy",
                table: "EvidenceFiles",
                newName: "UploadedByAccountId");

            migrationBuilder.RenameColumn(
                name: "LearnerId",
                table: "EvidenceFiles",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_EvidenceFiles_LearnerId",
                table: "EvidenceFiles",
                newName: "IX_EvidenceFiles_AccountId");

            migrationBuilder.RenameColumn(
                name: "LearnerId",
                table: "CourseEnrollments",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseEnrollments_LearnerId_ClassId",
                table: "CourseEnrollments",
                newName: "IX_CourseEnrollments_AccountId_ClassId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AuditLogs",
                newName: "AccountId");

            migrationBuilder.RenameColumn(
                name: "LearnerId",
                table: "AttendanceRecords",
                newName: "ClassStudentId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecords_SessionId_LearnerId",
                table: "AttendanceRecords",
                newName: "IX_AttendanceRecords_SessionId_ClassStudentId");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceRecords_LearnerId",
                table: "AttendanceRecords",
                newName: "IX_AttendanceRecords_ClassStudentId");

            migrationBuilder.RenameColumn(
                name: "RecordedBy",
                table: "AssessmentResults",
                newName: "GradedByAccountId");

            migrationBuilder.RenameColumn(
                name: "LearnerId",
                table: "AssessmentResults",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_AssessmentResults_LearnerId",
                table: "AssessmentResults",
                newName: "IX_AssessmentResults_AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_AssessmentResults_AssessmentId_LearnerId",
                table: "AssessmentResults",
                newName: "IX_AssessmentResults_AssessmentId_AccountId");

            migrationBuilder.RenameColumn(
                name: "ActionBy",
                table: "ApprovalHistories",
                newName: "ActionByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectSignoffs_SignoffByAccountId",
                table: "SubjectSignoffs",
                column: "SignoffByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ConfirmedByAccountId",
                table: "Sessions",
                column: "ConfirmedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticalChecklistResults_VerifiedByAccountId",
                table: "PracticalChecklistResults",
                column: "VerifiedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_RequestedByAccountId",
                table: "ExportJobs",
                column: "RequestedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceFiles_UploadedByAccountId",
                table: "EvidenceFiles",
                column: "UploadedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceFiles_VerifiedByAccountId",
                table: "EvidenceFiles",
                column: "VerifiedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_GradedByAccountId",
                table: "AssessmentResults",
                column: "GradedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalHistories_ActionByAccountId",
                table: "ApprovalHistories",
                column: "ActionByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_DepartmentId",
                table: "Accounts",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_RoleId",
                table: "Accounts",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Username",
                table: "Accounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassStudents_AccountId",
                table: "ClassStudents",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassStudents_CourseEnrollmentId",
                table: "ClassStudents",
                column: "CourseEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL AND [Email] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_LearnerTypeId",
                table: "UserProfiles",
                column: "LearnerTypeId");

            migrationBuilder.Sql(@"
                -- 5. CẬP NHẬT LẠI KHÓA NGOẠI: Cộng 1.000.000 vào các AccountId (từ LearnerId cũ) để nó trỏ đúng người
                UPDATE CourseEnrollments SET AccountId = AccountId + 1000000 WHERE AccountId IS NOT NULL AND AccountId < 1000000;
                UPDATE EvidenceFiles SET AccountId = AccountId + 1000000 WHERE AccountId IS NOT NULL AND AccountId < 1000000;
                UPDATE AssessmentResults SET AccountId = AccountId + 1000000 WHERE AccountId IS NOT NULL AND AccountId < 1000000;

                -- 6. Sinh dữ liệu giả lập cho ClassStudents dựa trên AttendanceRecords để tránh lỗi Khóa ngoại
                SET IDENTITY_INSERT ClassStudents ON;
                INSERT INTO ClassStudents (ClassStudentId, CourseEnrollmentId, ClassId, AccountId, Status, CreatedAt, IsDeleted)
                SELECT DISTINCT ClassStudentId, 1, 0, ClassStudentId + 1000000, 'Active', GETDATE(), 0
                FROM AttendanceRecords;
                SET IDENTITY_INSERT ClassStudents OFF;
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalHistories_Accounts_ActionByAccountId",
                table: "ApprovalHistories",
                column: "ActionByAccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentResults_Accounts_AccountId",
                table: "AssessmentResults",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentResults_Accounts_GradedByAccountId",
                table: "AssessmentResults",
                column: "GradedByAccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_ClassStudents_ClassStudentId",
                table: "AttendanceRecords",
                column: "ClassStudentId",
                principalTable: "ClassStudents",
                principalColumn: "ClassStudentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_Accounts_AccountId",
                table: "CourseEnrollments",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceFiles_Accounts_AccountId",
                table: "EvidenceFiles",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceFiles_Accounts_UploadedByAccountId",
                table: "EvidenceFiles",
                column: "UploadedByAccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceFiles_Accounts_VerifiedByAccountId",
                table: "EvidenceFiles",
                column: "VerifiedByAccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExportJobs_Accounts_RequestedByAccountId",
                table: "ExportJobs",
                column: "RequestedByAccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PracticalChecklistResults_Accounts_VerifiedByAccountId",
                table: "PracticalChecklistResults",
                column: "VerifiedByAccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Accounts_ConfirmedByAccountId",
                table: "Sessions",
                column: "ConfirmedByAccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubjectResults_ETRCourseRecords_EtrId",
                table: "SubjectResults",
                column: "EtrId",
                principalTable: "ETRCourseRecords",
                principalColumn: "ETRCourseRecordId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubjectSignoffs_Accounts_SignoffByAccountId",
                table: "SubjectSignoffs",
                column: "SignoffByAccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // (Phần Down giữ nguyên để có thể Rollback)
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalHistories_Accounts_ActionByAccountId",
                table: "ApprovalHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentResults_Accounts_AccountId",
                table: "AssessmentResults");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentResults_Accounts_GradedByAccountId",
                table: "AssessmentResults");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_ClassStudents_ClassStudentId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_Accounts_AccountId",
                table: "CourseEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceFiles_Accounts_AccountId",
                table: "EvidenceFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceFiles_Accounts_UploadedByAccountId",
                table: "EvidenceFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceFiles_Accounts_VerifiedByAccountId",
                table: "EvidenceFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ExportJobs_Accounts_RequestedByAccountId",
                table: "ExportJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_PracticalChecklistResults_Accounts_VerifiedByAccountId",
                table: "PracticalChecklistResults");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Accounts_ConfirmedByAccountId",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_SubjectResults_ETRCourseRecords_EtrId",
                table: "SubjectResults");

            migrationBuilder.DropForeignKey(
                name: "FK_SubjectSignoffs_Accounts_SignoffByAccountId",
                table: "SubjectSignoffs");

            migrationBuilder.DropTable(
                name: "ClassStudents");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Accounts");

        }
    }
}