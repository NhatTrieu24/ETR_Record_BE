using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ResetAndSeedSystemData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ==========================================
            // 1. THE "WIPE" SCRIPT
            // ==========================================
            migrationBuilder.Sql(@"
                -- Disable all constraints
                EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

                -- Delete all data except MigrationsHistory
                EXEC sp_MSForEachTable '
                    IF ''?'' NOT LIKE ''%__EFMigrationsHistory%''
                    BEGIN
                        DELETE FROM ?
                    END
                ';

                -- Reseed all identity columns back to 0 (next inserted will be 1)
                EXEC sp_MSForEachTable '
                    IF ''?'' NOT LIKE ''%__EFMigrationsHistory%'' AND OBJECTPROPERTY(OBJECT_ID(''?''), ''TableHasIdentity'') = 1
                    BEGIN
                        DBCC CHECKIDENT (''?'', RESEED, 0)
                    END
                ';

                -- Re-enable all constraints
                EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
            ");

            // ==========================================
            // 2. THE "SEED" SCRIPT
            // ==========================================
            migrationBuilder.Sql(@"
                DECLARE @Now DATETIME2 = GETUTCDATE();

                -- Seed Departments
                SET IDENTITY_INSERT [Departments] ON;
                INSERT INTO [Departments] (DepartmentId, DepartmentName, Description, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'Admin', 'System Administration', @Now, 0, NULL),
                (2, 'IT', 'Information Technology', @Now, 0, NULL),
                (3, 'HR', 'Human Resources', @Now, 0, NULL),
                (4, 'CRO', 'Course Registration Office', @Now, 0, NULL);
                SET IDENTITY_INSERT [Departments] OFF;

                -- Seed Roles
                SET IDENTITY_INSERT [Roles] ON;
                INSERT INTO [Roles] (RoleId, RoleName, Description, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'Admin', 'System Administrator', @Now, 0, NULL),
                (2, 'CROStaff', 'CRO Staff handling scheduling', @Now, 0, NULL),
                (3, 'Instructor', 'Course Instructor', @Now, 0, NULL),
                (4, 'Mentor', 'Mentor for practical checklists', @Now, 0, NULL),
                (5, 'Student', 'Student / Learner', @Now, 0, NULL);
                SET IDENTITY_INSERT [Roles] OFF;

                -- Seed Learner Types
                SET IDENTITY_INSERT [LearnerTypes] ON;
                INSERT INTO [LearnerTypes] (LearnerTypeId, TypeName, Description, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'Internal', 'Internal Employee', @Now, 0, NULL),
                (2, 'External', 'External Contractor', @Now, 0, NULL);
                SET IDENTITY_INSERT [LearnerTypes] OFF;

                -- Seed Accounts (Admin, CRO, Instructor, Mentor, Student)
                SET IDENTITY_INSERT [Accounts] ON;
                INSERT INTO [Accounts] (AccountId, Username, PasswordHash, RoleId, DepartmentId, Status, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'admin@system.com', 'hashed_password_123', 1, 1, 'Active', @Now, 0, 1),
                (2, 'cro@system.com', 'hashed_password_123', 2, 4, 'Active', @Now, 0, 1),
                (3, 'instructor@system.com', 'hashed_password_123', 3, 2, 'Active', @Now, 0, 1),
                (4, 'mentor@system.com', 'hashed_password_123', 4, 2, 'Active', @Now, 0, 1),
                (1000001, 'student@system.com', 'hashed_password_123', 5, 3, 'Active', @Now, 0, 1);
                SET IDENTITY_INSERT [Accounts] OFF;

                -- Seed UserProfiles (no identity for profile since it's 1-to-1 but EF might or might not have identity. Actually AccountId is PK so it doesn't have identity. Wait, in EF if AccountId is PK and FK, it's not identity.)
                INSERT INTO [UserProfiles] (AccountId, UserCode, FullName, Email, DateOfBirth, Gender, LearnerTypeId, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'ADM-001', 'System Admin', 'admin@system.com', '1980-01-01', 'Other', NULL, @Now, 0, 1),
                (2, 'CRO-001', 'CRO Staff', 'cro@system.com', '1990-01-01', 'Female', NULL, @Now, 0, 1),
                (3, 'INS-001', 'Senior Instructor', 'instructor@system.com', '1985-01-01', 'Male', NULL, @Now, 0, 1),
                (4, 'MNT-001', 'Technical Mentor', 'mentor@system.com', '1988-01-01', 'Female', NULL, @Now, 0, 1),
                (1000001, 'STU-001', 'Jane Student', 'student@system.com', '2000-01-01', 'Female', 1, @Now, 0, 1);

                -- Seed Course & Subjects
                SET IDENTITY_INSERT [Courses] ON;
                INSERT INTO [Courses] (CourseId, CourseCode, CourseName, DurationHours, Status, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'CRS-2026', 'Aviation ETR Bootcamp', 120, 'Active', @Now, 0, 1);
                SET IDENTITY_INSERT [Courses] OFF;

                SET IDENTITY_INSERT [Subjects] ON;
                INSERT INTO [Subjects] (SubjectId, SubjectCode, SubjectName, SubjectType, DefaultHours, Status, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'SUB-01', 'Safety Fundamentals', 'Theory', 40, 'Active', @Now, 0, 1),
                (2, 'SUB-02', 'Practical Emergency', 'Practical', 80, 'Active', @Now, 0, 1);
                SET IDENTITY_INSERT [Subjects] OFF;

                -- CourseSubjects has composite PK, NO Identity Insert
                INSERT INTO [CourseSubjects] (CourseId, SubjectId, SequenceNo, RequiredHours, PassingScore, IsMandatory, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 1, 1, 40, 75.0, 1, @Now, 0, 1),
                (1, 2, 2, 80, 80.0, 1, @Now, 0, 1);

                -- Seed Class
                SET IDENTITY_INSERT [Classes] ON;
                INSERT INTO [Classes] (ClassId, ClassCode, ClassName, CourseId, StartDate, EndDate, Capacity, Status, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'CLS-2026A', 'Summer Aviation 2026', 1, DATEADD(day, 1, @Now), DATEADD(day, 30, @Now), 30, 'Scheduled', @Now, 0, 1);
                SET IDENTITY_INSERT [Classes] OFF;

                -- Seed CourseEnrollment & ClassStudent
                SET IDENTITY_INSERT [CourseEnrollments] ON;
                INSERT INTO [CourseEnrollments] (EnrollmentId, AccountId, ClassId, EnrolledAt, Status, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 1000001, 1, @Now, 'Active', @Now, 0, 1);
                SET IDENTITY_INSERT [CourseEnrollments] OFF;

                SET IDENTITY_INSERT [ClassStudents] ON;
                INSERT INTO [ClassStudents] (ClassStudentId, CourseEnrollmentId, AccountId, ClassId, Status, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 1, 1000001, 1, 'Active', @Now, 0, 1);
                SET IDENTITY_INSERT [ClassStudents] OFF;

                -- Seed ETRCourseRecord & SubjectResults
                SET IDENTITY_INSERT [ETRCourseRecords] ON;
                INSERT INTO [ETRCourseRecords] (ETRCourseRecordId, EnrollmentId, FinalScore, FinalResult, Remarks, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 1, NULL, 'In Progress', 'Initial ETR Creation', @Now, 0, 1);
                SET IDENTITY_INSERT [ETRCourseRecords] OFF;

                SET IDENTITY_INSERT [SubjectResults] ON;
                INSERT INTO [SubjectResults] (SubjectResultId, EtrId, CourseId, SubjectId, Status, Score, Result, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 1, 1, 1, 'In Progress', NULL, 'Pending', @Now, 0, 1),
                (2, 1, 1, 2, 'Not Started', NULL, 'Pending', @Now, 0, 1);
                SET IDENTITY_INSERT [SubjectResults] OFF;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
