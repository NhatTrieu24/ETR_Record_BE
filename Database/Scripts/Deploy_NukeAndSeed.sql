
-- 1. THE WIPE
SET QUOTED_IDENTIFIER ON;

-- Disable all constraints
EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

-- Delete data from all tables explicitly to bypass QUOTED_IDENTIFIER scope issues
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

-- Reseed identity tables to 0
DECLARE @reseedSql NVARCHAR(MAX) = N'';
SELECT @reseedSql += N'DBCC CHECKIDENT (''' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N''', RESEED, 0);' + CHAR(13)
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.is_ms_shipped = 0 AND t.name <> '__EFMigrationsHistory' AND OBJECTPROPERTY(t.object_id, 'TableHasIdentity') = 1;

EXEC sp_executesql @reseedSql;

-- (Constraint check moved to the end)

-- 2. THE SEED
DECLARE @Now DATETIME2 = GETUTCDATE();
DECLARE @MockPass NVARCHAR(255) = '123456'; -- Plain text mock password

-- Roles
SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] (RoleId, RoleName, Description, CreatedAt, IsDeleted)
VALUES 
(1, 'Admin', 'System Administrator', @Now, 0),
(2, 'CROStaff', 'CRO Staff Member', @Now, 0),
(3, 'Instructor', 'Course Instructor', @Now, 0),
(4, 'Mentor', 'Student Mentor', @Now, 0),
(5, 'Student', 'Student/Learner', @Now, 0);
SET IDENTITY_INSERT [Roles] OFF;

-- Departments
SET IDENTITY_INSERT [Departments] ON;
INSERT INTO [Departments] (DepartmentId, DepartmentName, Description, CreatedAt, IsDeleted)
VALUES
(1, 'Admin', 'Administration Department', @Now, 0),
(2, 'IT', 'Information Technology', @Now, 0),
(3, 'HR', 'Human Resources', @Now, 0),
(4, 'CRO', 'Course Record Office', @Now, 0);
SET IDENTITY_INSERT [Departments] OFF;

-- Accounts
SET IDENTITY_INSERT [Accounts] ON;
INSERT INTO [Accounts] (AccountId, Username, PasswordHash, Status, RoleId, DepartmentId, CreatedAt, IsDeleted)
VALUES
(1, 'admin@etr.com', @MockPass, 'Active', 1, 1, @Now, 0),
(2, 'cro@etr.com', @MockPass, 'Active', 2, 4, @Now, 0),
(3, 'instructor@etr.com', @MockPass, 'Active', 3, 2, @Now, 0),
(4, 'mentor@etr.com', @MockPass, 'Active', 4, 2, @Now, 0),
(5, 'student@etr.com', @MockPass, 'Active', 5, 3, @Now, 0);
SET IDENTITY_INSERT [Accounts] OFF;

-- User Profiles (No Identity, uses AccountId as PK)
INSERT INTO [UserProfiles] (AccountId, UserCode, FullName, Email, Phone, DateOfBirth, Gender, Organization, CreatedAt, CreatedByAccountId, IsDeleted)
VALUES
(1, 'ADM-01', 'Admin User', 'admin@etr.com', '1234567890', '1990-01-01', 'Other', 'ETR', @Now, 1, 0),
(2, 'CRO-01', 'CRO User', 'cro@etr.com', '1234567891', '1992-01-01', 'Other', 'ETR', @Now, 1, 0),
(3, 'INS-01', 'Instructor User', 'instructor@etr.com', '1234567892', '1985-01-01', 'Other', 'ETR', @Now, 1, 0),
(4, 'MEN-01', 'Mentor User', 'mentor@etr.com', '1234567893', '1988-01-01', 'Other', 'ETR', @Now, 1, 0),
(5, 'STU-01', 'Student User', 'student@etr.com', '1234567894', '2000-01-01', 'Other', 'ETR', @Now, 1, 0);

-- Courses
SET IDENTITY_INSERT [Courses] ON;
INSERT INTO [Courses] (CourseId, CourseCode, CourseName, Description, DurationHours, Status, CreatedAt, CreatedByAccountId, IsDeleted)
VALUES
(1, 'C-NET-101', '.NET Backend Mastery', 'A complete guide to .NET backend development.', 120, 'Active', @Now, 1, 0),
(2, 'C-FE-102', 'Frontend Architecture', 'Advanced frontend design.', 80, 'Active', @Now, 1, 0);
SET IDENTITY_INSERT [Courses] OFF;

-- Subjects
SET IDENTITY_INSERT [Subjects] ON;
INSERT INTO [Subjects] (SubjectId, SubjectCode, SubjectName, SubjectType, DefaultHours, Status, CreatedAt, CreatedByAccountId, IsDeleted)
VALUES
(1, 'SUB-CS', 'C# Fundamentals', 'Theory', 40, 'Active', @Now, 1, 0),
(2, 'SUB-EF', 'Entity Framework Core', 'Theory', 40, 'Active', @Now, 1, 0),
(3, 'SUB-API', 'Web API Development', 'Practical', 40, 'Active', @Now, 1, 0);
SET IDENTITY_INSERT [Subjects] OFF;

-- CourseSubjects (Composite Key: CourseId, SubjectId)
INSERT INTO [CourseSubjects] (CourseId, SubjectId, SequenceNo, RequiredHours, PassingScore, IsMandatory, CreatedAt, CreatedByAccountId, IsDeleted)
VALUES
(1, 1, 1, 40, 50.00, 1, @Now, 1, 0),
(1, 2, 2, 40, 50.00, 1, @Now, 1, 0),
(1, 3, 3, 40, 50.00, 1, @Now, 1, 0);

-- Classes
SET IDENTITY_INSERT [Classes] ON;
INSERT INTO [Classes] (ClassId, ClassCode, ClassName, CourseId, StartDate, EndDate, Capacity, Status, CreatedAt, CreatedByAccountId, IsDeleted)
VALUES
(1, 'CLS-2026', '.NET Summer Bootcamp', 1, DATEADD(day, 7, @Now), DATEADD(day, 37, @Now), 30, 'Scheduled', @Now, 1, 0);
SET IDENTITY_INSERT [Classes] OFF;

-- Course Enrollments
SET IDENTITY_INSERT [CourseEnrollments] ON;
INSERT INTO [CourseEnrollments] (EnrollmentId, AccountId, ClassId, Status, EnrollmentDate, CompletionDate, CreatedAt, CreatedByAccountId, IsDeleted)
VALUES
(1, 5, 1, 'Active', @Now, NULL, @Now, 1, 0);
SET IDENTITY_INSERT [CourseEnrollments] OFF;

-- ClassStudents
SET IDENTITY_INSERT [ClassStudents] ON;
INSERT INTO [ClassStudents] (ClassStudentId, CourseEnrollmentId, AccountId, Status, CreatedAt, CreatedByAccountId, IsDeleted)
VALUES
(1, 1, 5, 'Active', @Now, 1, 0);
SET IDENTITY_INSERT [ClassStudents] OFF;

-- ETR Course Records
SET IDENTITY_INSERT [ETRCourseRecords] ON;
INSERT INTO [ETRCourseRecords] (ETRCourseRecordId, EnrollmentId, TotalScore, FinalGrade, Remarks, CreatedAt, CreatedByAccountId, IsDeleted)
VALUES
(1, 1, 0.0, 'In Progress', 'Just started', @Now, 1, 0);
SET IDENTITY_INSERT [ETRCourseRecords] OFF;

-- Subject Results
SET IDENTITY_INSERT [SubjectResults] ON;
INSERT INTO [SubjectResults] (SubjectResultId, EtrId, CourseId, SubjectId, AttendanceRate, Score, Status, Remarks, CreatedAt, CreatedByAccountId, IsDeleted)
VALUES
(1, 1, 1, 1, 100.00, 85.00, 'Passed', 'Good job', @Now, 1, 0),
(2, 1, 1, 2, 0.00, 0.00, 'NotStarted', NULL, @Now, 1, 0);
SET IDENTITY_INSERT [SubjectResults] OFF;

-- 3. RE-ENABLE CONSTRAINTS
EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
            
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260719015135_CleanUpAndSeedV2', N'9.0.0');
GO

