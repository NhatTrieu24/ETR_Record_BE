BEGIN TRANSACTION;

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
            

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260718064449_ResetAndSeedSystemData', N'9.0.0');

ALTER TABLE [UserProfiles] DROP CONSTRAINT [FK_UserProfiles_LearnerTypes_LearnerTypeId];

DROP TABLE [LearnerTypes];

DROP INDEX [IX_UserProfiles_LearnerTypeId] ON [UserProfiles];

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UserProfiles]') AND [c].[name] = N'LearnerTypeId');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [UserProfiles] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [UserProfiles] DROP COLUMN [LearnerTypeId];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260718083404_PurgeLegacySchemaAndSeeds', N'9.0.0');


SET QUOTED_IDENTIFIER ON;

EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

DELETE FROM [ApprovalHistories];
DELETE FROM [ApprovalRequests];
DELETE FROM [AssessmentResults];
DELETE FROM [Assessments];
DELETE FROM [AttendanceRecords];
DELETE FROM [ClassStudents];
DELETE FROM [CompletionRequirements];
DELETE FROM [CourseEnrollments];
DELETE FROM [CourseSubjects];
DELETE FROM [Classes];
DELETE FROM [Courses];
DELETE FROM [DashboardSnapshots];
DELETE FROM [ETRCourseRecords];
DELETE FROM [EvidenceFiles];
DELETE FROM [EvidenceTypes];
DELETE FROM [ExportJobs];
DELETE FROM [PracticalChecklistResults];
DELETE FROM [PracticalChecklists];
DELETE FROM [RetakeHistories];
DELETE FROM [Roles];
DELETE FROM [Sessions];
DELETE FROM [SubjectResults];
DELETE FROM [SubjectSignoffs];
DELETE FROM [Subjects];
DELETE FROM [UserProfiles];
DELETE FROM [Accounts];
DELETE FROM [Departments];
DELETE FROM [AuditLogs];

DECLARE @reseedSql NVARCHAR(MAX) = N'';
SELECT @reseedSql += N'DBCC CHECKIDENT (''' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N''', RESEED, 0);' + CHAR(13)
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.is_ms_shipped = 0 AND t.name <> '__EFMigrationsHistory' AND OBJECTPROPERTY(t.object_id, 'TableHasIdentity') = 1;
EXEC sp_executesql @reseedSql;

DECLARE @Now DATETIME2 = GETUTCDATE();
DECLARE @MockPass NVARCHAR(255) = '123456'; 

SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] (RoleId, RoleName, Description, CreatedAt, IsDeleted) VALUES 
(1, 'Admin', 'System Administrator', @Now, 0),
(2, 'CROStaff', 'CRO Staff Member', @Now, 0),
(3, 'Instructor', 'Course Instructor', @Now, 0),
(4, 'Mentor', 'Student Mentor', @Now, 0),
(5, 'Student', 'Student/Learner', @Now, 0);
SET IDENTITY_INSERT [Roles] OFF;

SET IDENTITY_INSERT [Departments] ON;
INSERT INTO [Departments] (DepartmentId, DepartmentName, Description, CreatedAt, IsDeleted) VALUES
(1, 'Admin', 'Administration Department', @Now, 0),
(2, 'IT', 'Information Technology', @Now, 0),
(3, 'HR', 'Human Resources', @Now, 0),
(4, 'CRO', 'Course Record Office', @Now, 0);
SET IDENTITY_INSERT [Departments] OFF;

SET IDENTITY_INSERT [Accounts] ON;
INSERT INTO [Accounts] (AccountId, Username, PasswordHash, Status, RoleId, DepartmentId, CreatedAt, IsDeleted) VALUES
(1, 'admin@etr.com', @MockPass, 'Active', 1, 1, @Now, 0),
(2, 'cro@etr.com', @MockPass, 'Active', 2, 4, @Now, 0),
(3, 'instructor@etr.com', @MockPass, 'Active', 3, 2, @Now, 0),
(4, 'mentor@etr.com', @MockPass, 'Active', 4, 2, @Now, 0),
(5, 'student@etr.com', @MockPass, 'Active', 5, 3, @Now, 0);
SET IDENTITY_INSERT [Accounts] OFF;

INSERT INTO [UserProfiles] (AccountId, UserCode, FullName, Email, Phone, DateOfBirth, Gender, Organization, CreatedAt, CreatedByAccountId, IsDeleted) VALUES
(1, 'ADM-01', 'Admin User', 'admin@etr.com', '1234567890', '1990-01-01', 'Other', 'ETR', @Now, 1, 0),
(2, 'CRO-01', 'CRO User', 'cro@etr.com', '1234567891', '1992-01-01', 'Other', 'ETR', @Now, 1, 0),
(3, 'INS-01', 'Instructor User', 'instructor@etr.com', '1234567892', '1985-01-01', 'Other', 'ETR', @Now, 1, 0),
(4, 'MEN-01', 'Mentor User', 'mentor@etr.com', '1234567893', '1988-01-01', 'Other', 'ETR', @Now, 1, 0),
(5, 'STU-01', 'Student User', 'student@etr.com', '1234567894', '2000-01-01', 'Other', 'ETR', @Now, 1, 0);

SET IDENTITY_INSERT [Courses] ON;
INSERT INTO [Courses] (CourseId, CourseCode, CourseName, Description, DurationHours, Status, CreatedAt, CreatedByAccountId, IsDeleted) VALUES
(1, 'C-NET-101', '.NET Backend Mastery', 'A complete guide to .NET backend development.', 120, 'Active', @Now, 1, 0),
(2, 'C-FE-102', 'Frontend Architecture', 'Advanced frontend design.', 80, 'Active', @Now, 1, 0);
SET IDENTITY_INSERT [Courses] OFF;

SET IDENTITY_INSERT [Subjects] ON;
INSERT INTO [Subjects] (SubjectId, SubjectCode, SubjectName, SubjectType, DefaultHours, Status, CreatedAt, CreatedByAccountId, IsDeleted) VALUES
(1, 'SUB-CS', 'C# Fundamentals', 'Theory', 40, 'Active', @Now, 1, 0),
(2, 'SUB-EF', 'Entity Framework Core', 'Theory', 40, 'Active', @Now, 1, 0),
(3, 'SUB-API', 'Web API Development', 'Practical', 40, 'Active', @Now, 1, 0);
SET IDENTITY_INSERT [Subjects] OFF;

INSERT INTO [CourseSubjects] (CourseId, SubjectId, SequenceNo, RequiredHours, PassingScore, IsMandatory, CreatedAt, CreatedByAccountId, IsDeleted) VALUES
(1, 1, 1, 40, 50.00, 1, @Now, 1, 0),
(1, 2, 2, 40, 50.00, 1, @Now, 1, 0),
(1, 3, 3, 40, 50.00, 1, @Now, 1, 0);

SET IDENTITY_INSERT [Classes] ON;
INSERT INTO [Classes] (ClassId, ClassCode, ClassName, CourseId, StartDate, EndDate, Capacity, Status, CreatedAt, CreatedByAccountId, IsDeleted) VALUES
(1, 'CLS-2026', '.NET Summer Bootcamp', 1, DATEADD(day, 7, @Now), DATEADD(day, 37, @Now), 30, 'Scheduled', @Now, 1, 0);
SET IDENTITY_INSERT [Classes] OFF;

SET IDENTITY_INSERT [CourseEnrollments] ON;
INSERT INTO [CourseEnrollments] (EnrollmentId, AccountId, ClassId, Status, EnrolledAt, ActualCompletionDate, CreatedAt, CreatedByAccountId, IsDeleted) VALUES
(1, 5, 1, 'Active', @Now, NULL, @Now, 1, 0);
SET IDENTITY_INSERT [CourseEnrollments] OFF;

SET IDENTITY_INSERT [ClassStudents] ON;
INSERT INTO [ClassStudents] (ClassStudentId, CourseEnrollmentId, ClassId, AccountId, Status, CreatedAt, CreatedByAccountId, IsDeleted) VALUES
(1, 1, 1, 5, 'Active', @Now, 1, 0);
SET IDENTITY_INSERT [ClassStudents] OFF;

SET IDENTITY_INSERT [ETRCourseRecords] ON;
INSERT INTO [ETRCourseRecords] (ETRCourseRecordId, EnrollmentId, Status, IsLocked, CreatedBySystem, CreatedAt, CreatedByAccountId, IsDeleted) VALUES
(1, 1, 'InProgress', 0, 1, @Now, 1, 0);
SET IDENTITY_INSERT [ETRCourseRecords] OFF;

SET IDENTITY_INSERT [SubjectResults] ON;
INSERT INTO [SubjectResults] (SubjectResultId, EtrId, CourseId, SubjectId, AttendanceRate, Score, Status, CreatedAt, CreatedByAccountId, IsDeleted) VALUES
(1, 1, 1, 1, 100.00, 85.00, 'Passed', @Now, 1, 0),
(2, 1, 1, 2, 0.00, 0.00, 'Pending', @Now, 1, 0);
SET IDENTITY_INSERT [SubjectResults] OFF;

EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
    

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260719015135_CleanUpAndSeedV2', N'9.0.0');

COMMIT;
GO

