-- ==============================================================================
-- CAPSTONE PROJECT: ELECTRONIC TRAINING RECORD (ETR) SYSTEM IN AVIATION
-- MASTER DEMO DATABASE SEEDING SCRIPT
-- Schema: Microsoft SQL Server / Azure SQL (EF Core Code-First Compliant)
-- ==============================================================================

-- 1. DYNAMICALLY DISABLE ALL FOREIGN KEYS (Azure SQL Supported)
DECLARE @sqlDisable NVARCHAR(MAX) = N'';
SELECT @sqlDisable += N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' NOCHECK CONSTRAINT ALL; '
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.name != '__EFMigrationsHistory';
EXEC sp_executesql @sqlDisable;

-- 2. PURGE OLD DATA IN REVERSE DEPENDENCY ORDER
DELETE FROM [dbo].[AuditLogs];
DELETE FROM [dbo].[Attachments];
DELETE FROM [dbo].[ExportJobs];
DELETE FROM [dbo].[ApprovalHistories];
DELETE FROM [dbo].[ApprovalRequests];
DELETE FROM [dbo].[AmendmentRequests];
DELETE FROM [dbo].[EvidenceFiles];
DELETE FROM [dbo].[EvidenceTypes];
DELETE FROM [dbo].[RetakeHistories];
DELETE FROM [dbo].[AttendanceRecords];
DELETE FROM [dbo].[AssessmentResults];
DELETE FROM [dbo].[PracticalChecklistResults];
DELETE FROM [dbo].[Sessions];
DELETE FROM [dbo].[Assessments];
DELETE FROM [dbo].[PracticalChecklists];
DELETE FROM [dbo].[SubjectSignoffs];
DELETE FROM [dbo].[SubjectResults];
DELETE FROM [dbo].[ETRCourseRecords];
DELETE FROM [dbo].[CourseEnrollments];
DELETE FROM [dbo].[ClassSubjects];
DELETE FROM [dbo].[Classes];
DELETE FROM [dbo].[CompletionRequirements];
DELETE FROM [dbo].[CourseSubjects];
DELETE FROM [dbo].[Subjects];
DELETE FROM [dbo].[Courses];
DELETE FROM [dbo].[UserProfiles];
DELETE FROM [dbo].[Accounts];
DELETE FROM [dbo].[Departments];
DELETE FROM [dbo].[Roles];

-- 3. RESEED IDENTITIES TO ZERO
DBCC CHECKIDENT ('[dbo].[AuditLogs]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Attachments]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[ExportJobs]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[ApprovalHistories]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[ApprovalRequests]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[AmendmentRequests]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[EvidenceFiles]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[EvidenceTypes]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[RetakeHistories]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[AttendanceRecords]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[AssessmentResults]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[PracticalChecklistResults]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Sessions]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Assessments]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[PracticalChecklists]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[SubjectSignoffs]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[SubjectResults]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[ETRCourseRecords]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[CourseEnrollments]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[ClassSubjects]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Classes]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[CompletionRequirements]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Subjects]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Courses]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Accounts]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Departments]', RESEED, 0);
DBCC CHECKIDENT ('[dbo].[Roles]', RESEED, 0);

-- ==============================================================================
-- 4. INSERT MASTER SEED DATA
-- ==============================================================================

-- 4.1 Roles
SET IDENTITY_INSERT [dbo].[Roles] ON;
INSERT INTO [dbo].[Roles] (RoleId, RoleName, Description, CreatedAt, IsDeleted) VALUES
(1, N'Admin', N'System Administrator with full security and audit management privileges', SYSUTCDATETIME(), 0),
(2, N'Instructor', N'Flight & Ground Training Instructor authorized for grading and signoff', SYSUTCDATETIME(), 0),
(3, N'QA', N'Quality Assurance Officer verifying regulatory and academy compliance', SYSUTCDATETIME(), 0),
(4, N'Academic', N'Academic Affairs Officer managing schedules, classes, and student enrollments', SYSUTCDATETIME(), 0),
(5, N'TrainingManager', N'Training Director / Head of Training with final ETR approval rights', SYSUTCDATETIME(), 0),
(6, N'Student', N'Aviation Trainee / Cadet Pilot undergoing flight or technical training', SYSUTCDATETIME(), 0),
(7, N'Audit', N'CAAV / EASA Regulatory Auditor with strict read-only audit oversight', SYSUTCDATETIME(), 0),
(8, N'ManagementViewer', N'Academy Executive Viewer with aggregate dashboard visibility', SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[Roles] OFF;

-- 4.2 Departments (Active, IsDeleted = 0)
SET IDENTITY_INSERT [dbo].[Departments] ON;
INSERT INTO [dbo].[Departments] (DepartmentId, DepartmentName, Description, CreatedAt, IsDeleted) VALUES
(1, N'Administration', N'Central Academy Administration, Registry and Compliance', SYSUTCDATETIME(), 0),
(2, N'Training', N'Flight, Ground and Simulator Training Operations Department', SYSUTCDATETIME(), 0),
(3, N'Flight Crew', N'Cockpit Operations, Cadet Pilots and Chief Flight Instructors', SYSUTCDATETIME(), 0),
(4, N'Cabin Crew', N'In-flight Safety, Emergency Procedures and Cabin Service', SYSUTCDATETIME(), 0),
(5, N'Engineering & Maintenance', N'Aircraft Maintenance Engineering and Technical Operations', SYSUTCDATETIME(), 0),
(6, N'Ground Operations', N'Airport Ramp, Dispatch and Ground Handling Operations', SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[Departments] OFF;

-- 4.3 Accounts (Password: 123456 -> BCrypt hash)
SET IDENTITY_INSERT [dbo].[Accounts] ON;
INSERT INTO [dbo].[Accounts] (AccountId, Username, PasswordHash, RoleId, DepartmentId, Status, CreatedAt, IsDeleted) VALUES
(1, N'admin@etr.com', N'$2a$11$L8am0SkeldYznhz7ElBDguSAfGHZt7uE5rkJ9s6oZNNt.joBDdeY6', 1, 1, N'Active', SYSUTCDATETIME(), 0),
(2, N'instructor@etr.com', N'$2a$11$L8am0SkeldYznhz7ElBDguSAfGHZt7uE5rkJ9s6oZNNt.joBDdeY6', 2, 2, N'Active', SYSUTCDATETIME(), 0),
(3, N'qa@etr.com', N'$2a$11$L8am0SkeldYznhz7ElBDguSAfGHZt7uE5rkJ9s6oZNNt.joBDdeY6', 3, 1, N'Active', SYSUTCDATETIME(), 0),
(4, N'academic@etr.com', N'$2a$11$L8am0SkeldYznhz7ElBDguSAfGHZt7uE5rkJ9s6oZNNt.joBDdeY6', 4, 1, N'Active', SYSUTCDATETIME(), 0),
(5, N'student@etr.com', N'$2a$11$L8am0SkeldYznhz7ElBDguSAfGHZt7uE5rkJ9s6oZNNt.joBDdeY6', 6, 2, N'Active', SYSUTCDATETIME(), 0),
(6, N'manager@etr.com', N'$2a$11$L8am0SkeldYznhz7ElBDguSAfGHZt7uE5rkJ9s6oZNNt.joBDdeY6', 5, 2, N'Active', SYSUTCDATETIME(), 0),
(7, N'auditor@etr.com', N'$2a$11$L8am0SkeldYznhz7ElBDguSAfGHZt7uE5rkJ9s6oZNNt.joBDdeY6', 7, 1, N'Active', SYSUTCDATETIME(), 0),
(8, N'viewer@etr.com', N'$2a$11$L8am0SkeldYznhz7ElBDguSAfGHZt7uE5rkJ9s6oZNNt.joBDdeY6', 8, 1, N'Active', SYSUTCDATETIME(), 0),
(9, N'student2@etr.com', N'$2a$11$L8am0SkeldYznhz7ElBDguSAfGHZt7uE5rkJ9s6oZNNt.joBDdeY6', 6, 2, N'Active', SYSUTCDATETIME(), 0),
(10, N'student3@etr.com', N'$2a$11$L8am0SkeldYznhz7ElBDguSAfGHZt7uE5rkJ9s6oZNNt.joBDdeY6', 6, 2, N'Active', SYSUTCDATETIME(), 0),
(11, N'student4@etr.com', N'$2a$11$L8am0SkeldYznhz7ElBDguSAfGHZt7uE5rkJ9s6oZNNt.joBDdeY6', 6, 2, N'Active', SYSUTCDATETIME(), 0),
(12, N'instructor2@etr.com', N'$2a$11$L8am0SkeldYznhz7ElBDguSAfGHZt7uE5rkJ9s6oZNNt.joBDdeY6', 2, 2, N'Active', SYSUTCDATETIME(), 0),
(13, N'student5@etr.com', N'$2a$11$L8am0SkeldYznhz7ElBDguSAfGHZt7uE5rkJ9s6oZNNt.joBDdeY6', 6, 2, N'Active', SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[Accounts] OFF;

-- 4.4 UserProfiles
INSERT INTO [dbo].[UserProfiles] (
    AccountId, UserCode, FullName, Email, Phone, DateOfBirth, Gender, Organization,
    Status, CreatedAt, IsDeleted, IsCredentialsVerified,
    LicenseNumber, LicenseType, LicenseExpiryDate,
    MedicalClass, MedicalExpiryDate,
    IcaoElpLevel, IcaoElpExpiryDate,
    TypeRatings
) VALUES
-- Admin
(1, N'ADM-001', N'System Administrator', N'admin@etr.com', N'0901000001', '1985-01-01', N'Male', N'ETR Aviation Academy', N'Active', SYSUTCDATETIME(), 0, 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),

-- Instructor 1: Valid credentials
(2, N'INS-001', N'Capt. Tran Van Thanh', N'instructor@etr.com', N'0902000002', '1980-05-15', N'Male', N'ETR Aviation Academy', N'Active', SYSUTCDATETIME(), 0, 1, N'ATPL-VN-88421', N'ATPL', '2027-05-15', N'Class 1', '2027-05-15', 6, NULL, N'A320/A321, B737'),

-- QA & Academic
(3, N'QA-001', N'Le Thi Mai', N'qa@etr.com', N'0903000003', '1988-08-20', N'Female', N'ETR Aviation Academy', N'Active', SYSUTCDATETIME(), 0, 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
(4, N'ACA-001', N'Nguyen Hoang Long', N'academic@etr.com', N'0904000004', '1990-03-12', N'Male', N'ETR Aviation Academy', N'Active', SYSUTCDATETIME(), 0, 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),

-- Student 1 (Jane Student): Medical EXPIRED, ICAO ELP EXPIRING SOON!
(5, N'STU-01', N'Jane Student', N'student@etr.com', N'0905000005', '2001-06-18', N'Female', N'Cadet Pilot Wing', N'Active', SYSUTCDATETIME(), 0, 1, N'CPL-VN-10294', N'CPL', '2027-12-31', N'Class 1', '2026-08-15', 4, '2026-10-25', N'SEP, MEP'),

-- Manager, Auditor, Viewer
(6, N'MGR-001', N'Doan Minh Tri', N'manager@etr.com', N'0906000006', '1978-11-25', N'Male', N'ETR Aviation Academy', N'Active', SYSUTCDATETIME(), 0, 1, N'ATPL-VN-66512', N'ATPL', '2028-01-01', N'Class 1', '2027-06-30', 6, NULL, N'A320/A321, B777'),
(7, N'AUD-001', N'Pham Quang Huy', N'auditor@etr.com', N'0907000007', '1982-04-10', N'Male', N'Civil Aviation Authority of Vietnam', N'Active', SYSUTCDATETIME(), 0, 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
(8, N'USR-001', N'Vuong Dinh Khoi', N'viewer@etr.com', N'0908000008', '1992-09-09', N'Male', N'ETR Executive Board', N'Active', SYSUTCDATETIME(), 0, 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),

-- Student 2: ALL CREDENTIALS EXPIRED -> GROUNDED!
(9, N'STU-02', N'Student 2', N'student2@etr.com', N'0905000009', '2001-07-22', N'Male', N'Cadet Pilot Wing', N'Grounded', SYSUTCDATETIME(), 0, 0, N'CPL-VN-10295', N'CPL', '2026-06-30', N'Class 1', '2026-07-20', 4, '2026-05-10', N'SEP'),

-- Student 3: License EXPIRING SOON in 16 days!
(10, N'STU-03', N'Student 3', N'student3@etr.com', N'0905000010', '2002-02-14', N'Male', N'Cadet Pilot Wing', N'Active', SYSUTCDATETIME(), 0, 1, N'CPL-VN-10296', N'CPL', '2026-10-18', N'Class 1', '2027-04-15', 5, '2028-11-20', N'SEP'),

-- Student 4: Valid credentials
(11, N'STU-04', N'Student 4', N'student4@etr.com', N'0905000011', '2002-10-05', N'Male', N'Cadet Pilot Wing', N'Active', SYSUTCDATETIME(), 0, 1, N'CPL-VN-10297', N'CPL', '2027-09-30', N'Class 1', '2027-09-30', 6, NULL, N'SEP'),

-- Instructor 2: Medical EXPIRED, License EXPIRING SOON!
(12, N'INS-002', N'Capt. Nguyen Quoc Bao', N'instructor2@etr.com', N'0902000012', '1983-12-01', N'Male', N'ETR Aviation Academy', N'Active', SYSUTCDATETIME(), 0, 1, N'ATPL-VN-77312', N'ATPL', '2026-10-20', N'Class 1', '2026-09-01', 5, '2028-06-30', N'A320/A321'),

-- Student 5: Expiring Soon Certificate in 8 days
(13, N'STU-05', N'Student Five', N'student5@etr.com', N'0905000013', '2002-12-12', N'Female', N'Cadet Pilot Wing', N'Active', SYSUTCDATETIME(), 0, 1, N'CPL-VN-10298', N'CPL', '2027-05-15', N'Class 1', '2027-05-15', 5, '2028-01-01', N'SEP');

-- 4.5 Courses
SET IDENTITY_INSERT [dbo].[Courses] ON;
INSERT INTO [dbo].[Courses] (CourseId, CourseCode, CourseName, Description, DurationHours, Status, CourseType, ValidityMonths, EffectiveFrom, VersionNo, CreatedAt, IsDeleted) VALUES
(1, N'AMT-BASIC-2026', N'Aircraft Maintenance Technician Initial Qualification', N'Basic AMT certification approved under CAAV/EASA Part-147 standard.', 120, N'Active', N'Initial', 24, '2026-01-01', 1, SYSUTCDATETIME(), 0),
(2, N'A320-FAM', N'Airbus A320 Familiarization and Type Rating', N'A320 Family ground and simulator endorsement course.', 80, N'Active', N'TypeRating', 12, '2026-01-01', 1, SYSUTCDATETIME(), 0),
(3, N'B737-RATING', N'Boeing 737 Next Generation Type Rating', N'Full Flight Simulator and systems training for B737-800.', 90, N'Active', N'TypeRating', 12, '2026-01-01', 1, SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[Courses] OFF;

-- 4.6 Subjects
SET IDENTITY_INSERT [dbo].[Subjects] ON;
INSERT INTO [dbo].[Subjects] (SubjectId, SubjectCode, SubjectName, SubjectType, DefaultHours, AssessmentMethod, Description, Status, MinSessions, MaxSessions, CreatedAt, IsDeleted) VALUES
(1, N'SJ-REG', N'Aviation Regulations', N'Theory', 30, N'Exam', N'Civil Aviation Authority Regulations, ICAO Annexes, and Air Law.', N'Active', 6, 10, SYSUTCDATETIME(), 0),
(2, N'SJ-SYS', N'Aircraft Systems & Avionics', N'Theory', 40, N'Exam', N'Hydraulic, Electrical, Flight Controls and Avionics systems.', N'Active', 8, 12, SYSUTCDATETIME(), 0),
(3, N'SJ-PRA', N'Practical Line Maintenance & Troubleshooting', N'Practical', 30, N'Practical', N'Hands-on hangar inspection, pre-flight and component replacement.', N'Active', 6, 10, SYSUTCDATETIME(), 0),
(4, N'SJ-HF', N'Human Factors & Safety Management', N'Theory', 20, N'Exam', N'Error management, CRM principles, and aviation safety reporting.', N'Active', 4, 6, SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[Subjects] OFF;

-- 4.7 CourseSubjects (Composite PK: CourseId, SubjectId)
INSERT INTO [dbo].[CourseSubjects] (CourseId, SubjectId, SequenceNo, RequiredHours, PassingScore, IsMandatory, SubjectVersion, RequiredSessions, CreatedAt, IsDeleted) VALUES
-- Course 1: AMT-BASIC-2026
(1, 1, 1, 30, 70.00, 1, N'1.0', 6, SYSUTCDATETIME(), 0),
(1, 2, 2, 40, 75.00, 1, N'1.0', 8, SYSUTCDATETIME(), 0),
(1, 3, 3, 30, 80.00, 1, N'1.0', 6, SYSUTCDATETIME(), 0),
(1, 4, 4, 20, 70.00, 1, N'1.0', 4, SYSUTCDATETIME(), 0),
-- Course 2: A320-FAM (Required for Class Roster Import Demo)
(2, 2, 1, 40, 75.00, 1, N'1.0', 8, SYSUTCDATETIME(), 0),
(2, 3, 2, 40, 80.00, 1, N'1.0', 8, SYSUTCDATETIME(), 0),
-- Course 3: B737-RATING
(3, 2, 1, 50, 75.00, 1, N'1.0', 10, SYSUTCDATETIME(), 0),
(3, 3, 2, 40, 80.00, 1, N'1.0', 8, SYSUTCDATETIME(), 0);

-- 4.8 CompletionRequirements
SET IDENTITY_INSERT [dbo].[CompletionRequirements] ON;
INSERT INTO [dbo].[CompletionRequirements] (RequirementId, CourseId, RequirementName, Description, IsMandatory, DisplayOrder, RequirementType, ThresholdValue, VersionNo, EffectiveFrom, CreatedAt, IsDeleted) VALUES
(1, 1, N'Minimum Attendance Threshold (80%)', N'Mandatory 80% attendance across all course subjects', 1, 1, N'MinAttendance', 80.00, 1, '2026-01-01', SYSUTCDATETIME(), 0),
(2, 1, N'All Theory Assessments Passed', N'Pass all subject theory examinations above passing score threshold', 1, 2, N'AllAssessmentsPassed', NULL, 1, '2026-01-01', SYSUTCDATETIME(), 0),
(3, 1, N'Practical Line Maintenance Checklists Signed Off', N'All required practical checklists must be verified by instructor', 1, 3, N'AllChecklistsSignedOff', NULL, 1, '2026-01-01', SYSUTCDATETIME(), 0),
(4, 2, N'Minimum Attendance Threshold (80%)', N'Mandatory 80% attendance in A320 modules', 1, 1, N'MinAttendance', 80.00, 1, '2026-01-01', SYSUTCDATETIME(), 0),
(5, 2, N'All Assessments Passed', N'Pass all A320 simulator and system exams', 1, 2, N'AllAssessmentsPassed', NULL, 1, '2026-01-01', SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[CompletionRequirements] OFF;

-- 4.9 Classes
SET IDENTITY_INSERT [dbo].[Classes] ON;
INSERT INTO [dbo].[Classes] (ClassId, ClassCode, ClassName, CourseId, StartDate, EndDate, Location, Capacity, Status, CourseVersionNo, CreatedAt, IsDeleted) VALUES
(1, N'AMT-101-C1', N'AMT Initial Qualification Batch 2026-A', 1, '2026-08-01', '2026-11-30', N'Classroom 101 & Hangar A', 25, N'InProgress', 1, SYSUTCDATETIME(), 0),
(2, N'AMT-101-C2', N'AMT Initial Qualification Batch 2026-B', 1, '2026-03-01', '2026-06-30', N'Classroom 102 & Hangar B', 20, N'Completed', 1, SYSUTCDATETIME(), 0),
(3, N'B737-2026A', N'B737 NG Rating Batch 01', 3, '2026-11-01', '2027-02-28', N'Sim Center Bay 1', 16, N'Scheduled', 1, SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[Classes] OFF;

-- 4.10 ClassSubjects
SET IDENTITY_INSERT [dbo].[ClassSubjects] ON;
INSERT INTO [dbo].[ClassSubjects] (ClassSubjectId, ClassId, SubjectId, InstructorAccountId, CreatedAt, IsDeleted) VALUES
(1, 1, 1, 2, SYSUTCDATETIME(), 0),
(2, 1, 2, 2, SYSUTCDATETIME(), 0),
(3, 1, 3, 12, SYSUTCDATETIME(), 0),
(4, 1, 4, 2, SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[ClassSubjects] OFF;

-- 4.11 CourseEnrollments (Class 1 & Class 2)
SET IDENTITY_INSERT [dbo].[CourseEnrollments] ON;
INSERT INTO [dbo].[CourseEnrollments] (EnrollmentId, AccountId, ClassId, Status, EnrolledAt, StartDate, ExpectedCompletionDate, CreatedAt, IsDeleted) VALUES
(1, 5, 1, N'Active', '2026-08-01', '2026-08-01', '2026-11-30', SYSUTCDATETIME(), 0),
(2, 9, 1, N'Active', '2026-08-01', '2026-08-01', '2026-11-30', SYSUTCDATETIME(), 0),
(3, 10, 1, N'Active', '2026-08-01', '2026-08-01', '2026-11-30', SYSUTCDATETIME(), 0),
(4, 11, 1, N'Active', '2026-08-01', '2026-08-01', '2026-11-30', SYSUTCDATETIME(), 0),
-- Completed historical enrollments for Class 2 (AMT-101-C2) to demonstrate ETR validity/expiry
(5, 9, 2, N'Completed', '2024-01-10', '2024-01-15', '2024-06-30', '2024-01-10', 0),
(6, 10, 2, N'Completed', '2024-05-01', '2024-05-05', '2024-10-20', '2024-05-01', 0),
(7, 13, 2, N'Completed', '2024-05-01', '2024-05-05', '2024-10-10', '2024-05-01', 0);
SET IDENTITY_INSERT [dbo].[CourseEnrollments] OFF;

-- 4.12 ETRCourseRecords
SET IDENTITY_INSERT [dbo].[ETRCourseRecords] ON;
INSERT INTO [dbo].[ETRCourseRecords] (ETRCourseRecordId, EnrollmentId, Status, IsLocked, CreatedBySystem, CourseVersionNo, IssuedDate, ExpiryDate, CompletedAt, CreatedAt, IsDeleted) VALUES
(1, 1, N'UnderReview', 0, 1, 1, NULL, NULL, NULL, SYSUTCDATETIME(), 0),
(2, 2, N'UnderReview', 0, 1, 1, NULL, NULL, NULL, '2024-01-01', 0),
(3, 3, N'UnderReview', 0, 1, 1, NULL, NULL, NULL, '2024-01-01', 0),
(4, 4, N'UnderReview', 0, 1, 1, NULL, NULL, NULL, '2024-01-01', 0),
-- ETR 5: Completed in Class 2, EXPIRED on 2026-06-30 (Hết hạn > 3 tháng) -> Due for training!
(5, 5, N'Completed', 1, 1, 1, '2024-06-30', '2026-06-30', '2024-06-30', '2024-01-10', 0),
-- ETR 6: Completed in Class 2, EXPIRING on 2026-10-20 (Sắp hết hạn trong 18 ngày) -> Warning!
(6, 6, N'Completed', 1, 1, 1, '2024-10-20', '2026-10-20', '2024-10-20', '2024-05-01', 0),
-- ETR 7: Completed in Class 2, EXPIRING on 2026-10-10 (Sắp hết hạn trong 8 ngày) -> Warning!
(7, 7, N'Completed', 1, 1, 1, '2024-10-10', '2026-10-10', '2024-10-10', '2024-05-01', 0);
SET IDENTITY_INSERT [dbo].[ETRCourseRecords] OFF;

-- 4.13 SubjectResults (Subject 1: SJ-REG for ETR 1-4)
SET IDENTITY_INSERT [dbo].[SubjectResults] ON;
INSERT INTO [dbo].[SubjectResults] (
    SubjectResultId, EtrId, CourseId, SubjectId, AttendanceRate, Score, Status,
    PassingScoreSnapshot, SubjectCodeSnapshot, SubjectNameSnapshot, SubjectTypeSnapshot,
    RequiredHoursSnapshot, RequiredSessionsSnapshot, IsMandatorySnapshot, SequenceNoSnapshot, SubjectVersionSnapshot,
    CreatedAt, IsDeleted
) VALUES
(1, 1, 1, 1, 100.00, 85.00, N'Pending', 70.00, N'SJ-REG', N'Aviation Regulations', N'Theory', 30, 6, 1, 1, N'1.0', SYSUTCDATETIME(), 0),
(2, 2, 1, 1, 100.00, 90.00, N'Pending', 70.00, N'SJ-REG', N'Aviation Regulations', N'Theory', 30, 6, 1, 1, N'1.0', SYSUTCDATETIME(), 0),
(3, 3, 1, 1, 100.00, 72.00, N'Pending', 70.00, N'SJ-REG', N'Aviation Regulations', N'Theory', 30, 6, 1, 1, N'1.0', SYSUTCDATETIME(), 0),
(4, 4, 1, 1, 80.00, 80.00, N'Pending', 70.00, N'SJ-REG', N'Aviation Regulations', N'Theory', 30, 6, 1, 1, N'1.0', SYSUTCDATETIME(), 0),
-- Subject 2, 3, 4 for ETR 1
(5, 1, 1, 2, 100.00, 88.00, N'Passed', 75.00, N'SJ-SYS', N'Aircraft Systems & Avionics', N'Theory', 40, 8, 1, 2, N'1.0', SYSUTCDATETIME(), 0),
(6, 1, 1, 3, 100.00, 85.00, N'Passed', 80.00, N'SJ-PRA', N'Practical Line Maintenance & Troubleshooting', N'Practical', 30, 6, 1, 3, N'1.0', SYSUTCDATETIME(), 0),
(7, 1, 1, 4, 100.00, 92.00, N'Passed', 70.00, N'SJ-HF', N'Human Factors & Safety Management', N'Theory', 20, 4, 1, 4, N'1.0', SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[SubjectResults] OFF;

-- 4.14 Assessments (Assessment 1 matches Demo_Assessment_Import_Scores.xlsx)
SET IDENTITY_INSERT [dbo].[Assessments] ON;
INSERT INTO [dbo].[Assessments] (AssessmentId, CourseId, SubjectId, ComponentName, AssessmentType, Weight, PassingScore, IsRequired, DisplayOrder, CreatedAt, IsDeleted) VALUES
(1, 1, 1, N'Final Exam (Theory) - Subject: Aviation Regulations', N'TheoryExam', 100.00, 70.00, 1, 1, SYSUTCDATETIME(), 0),
(2, 1, 2, N'Aircraft Systems Midterm & Final Exam', N'TheoryExam', 100.00, 75.00, 1, 1, SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[Assessments] OFF;

-- 4.15 Sessions (Session 1 matches Demo_Attendance_Import_Session.xlsx and is UNCONFIRMED)
SET IDENTITY_INSERT [dbo].[Sessions] ON;
INSERT INTO [dbo].[Sessions] (
    SessionId, ClassId, SubjectId, SessionTitle, SessionDate, Location,
    IsConfirmed, IsAssessmentRequired, IsChecklistRequired, TrainingType, LessonCode,
    CreatedAt, IsDeleted
) VALUES
(1, 1, 1, N'Flight Operations Briefing - AMT-101-C1', '2026-10-15 08:00:00', N'Classroom 101', 0, 0, 0, N'Theory', N'AMT-SES-01', SYSUTCDATETIME(), 0),
(2, 1, 1, N'Aviation Law & CAAV Flight Standards', '2026-09-10 08:00:00', N'Classroom 101', 1, 0, 0, N'Theory', N'AMT-SES-02', SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[Sessions] OFF;

-- 4.16 AttendanceRecords (for confirmed Session 2)
SET IDENTITY_INSERT [dbo].[AttendanceRecords] ON;
INSERT INTO [dbo].[AttendanceRecords] (AttendanceRecordId, SessionId, EnrollmentId, Status, Remarks, RecordedByAccountId, RecordedAt, CreatedAt, IsDeleted) VALUES
(1, 2, 1, N'Present', N'On time, well prepared', 2, '2026-09-10 08:15:00', SYSUTCDATETIME(), 0),
(2, 2, 2, N'Present', N'Present and attentive', 2, '2026-09-10 08:15:00', SYSUTCDATETIME(), 0),
(3, 2, 3, N'Present', N'Present and attentive', 2, '2026-09-10 08:15:00', SYSUTCDATETIME(), 0),
(4, 2, 4, N'Present', N'Present and attentive', 2, '2026-09-10 08:15:00', SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[AttendanceRecords] OFF;

-- 4.17 PracticalChecklists
SET IDENTITY_INSERT [dbo].[PracticalChecklists] ON;
INSERT INTO [dbo].[PracticalChecklists] (PracticalChecklistId, CourseId, SubjectId, ItemName, Description, IsRequired, DisplayOrder, CreatedAt, IsDeleted) VALUES
(1, 1, 3, N'Pre-flight Aircraft Walkaround & Inspection Checklist', N'Verify airframe integrity, landing gear pins, pitot covers and fuel drain.', 1, 1, SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[PracticalChecklists] OFF;

-- 4.18 PracticalChecklistResults
SET IDENTITY_INSERT [dbo].[PracticalChecklistResults] ON;
INSERT INTO [dbo].[PracticalChecklistResults] (PracticalChecklistResultId, SessionId, SubjectResultId, PracticalChecklistId, Score, ResultStatus, VerifiedByAccountId, CompletedAt, IsPublished, PublishedAt, CreatedAt, IsDeleted, IsMandatorySnapshot) VALUES
(1, NULL, 6, 1, 95.00, N'Passed', 12, '2026-09-15 15:00:00', 1, '2026-09-15 15:30:00', SYSUTCDATETIME(), 0, 1);
SET IDENTITY_INSERT [dbo].[PracticalChecklistResults] OFF;

-- 4.19 EvidenceTypes
SET IDENTITY_INSERT [dbo].[EvidenceTypes] ON;
INSERT INTO [dbo].[EvidenceTypes] (EvidenceTypeId, TypeName, Description, CreatedAt, IsDeleted) VALUES
(1, N'Aircraft Maintenance Log (AML)', N'Standard aircraft journey and technical maintenance log sheet.', SYSUTCDATETIME(), 0),
(2, N'Flight Simulator Session Log', N'Certified printout of full flight simulator training session.', SYSUTCDATETIME(), 0),
(3, N'Theory Exam Score Sheet', N'Signed examination result sheet with proctor stamp.', SYSUTCDATETIME(), 0),
(4, N'Medical & English Proficiency', N'CAAV Class 1 Medical and ICAO ELP Level 4+ certificate.', SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[EvidenceTypes] OFF;

-- 4.20 SubjectSignoffs
SET IDENTITY_INSERT [dbo].[SubjectSignoffs] ON;
INSERT INTO [dbo].[SubjectSignoffs] (SubjectSignoffId, SubjectResultId, SignoffByAccountId, Role, SignoffAt, Comment, CreatedAt, IsDeleted) VALUES
(1, 5, 2, N'Instructor', '2026-09-20 10:00:00', N'Completed all avionics training requirements.', SYSUTCDATETIME(), 0),
(2, 6, 12, N'Instructor', '2026-09-20 10:30:00', N'Demonstrated competent line maintenance skills.', SYSUTCDATETIME(), 0),
(3, 7, 2, N'Instructor', '2026-09-20 11:00:00', N'Good CRM awareness and threat management.', SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[SubjectSignoffs] OFF;

-- 4.21 ApprovalRequests & ApprovalHistories
SET IDENTITY_INSERT [dbo].[ApprovalRequests] ON;
INSERT INTO [dbo].[ApprovalRequests] (ApprovalRequestId, ETRCourseRecordId, CurrentStatus, SubmittedByAccountId, SubmittedAt, CurrentApproverId, CreatedAt, IsDeleted) VALUES
(1, 1, N'UnderReview', 2, '2026-09-21 09:00:00', 3, SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[ApprovalRequests] OFF;

SET IDENTITY_INSERT [dbo].[ApprovalHistories] ON;
INSERT INTO [dbo].[ApprovalHistories] (ApprovalHistoryId, ApprovalRequestId, ActionByAccountId, ActionType, PreviousStatus, NewStatus, Comments, ActionAt, CreatedAt, IsDeleted) VALUES
(1, 1, 2, N'Submit', N'Draft', N'UnderReview', N'ETR compiled and submitted for QA verification.', '2026-09-21 09:00:00', SYSUTCDATETIME(), 0);
SET IDENTITY_INSERT [dbo].[ApprovalHistories] OFF;

-- 4.22 AuditLogs
SET IDENTITY_INSERT [dbo].[AuditLogs] ON;
INSERT INTO [dbo].[AuditLogs] (AuditLogId, AccountId, ActionType, EntityName, RecordId, Description, CreatedAt) VALUES
(1, 1, N'SYSTEM_SEED', N'Database', 1, N'Master Demo Database seeded successfully for ETR Capstone defense.', SYSUTCDATETIME());
SET IDENTITY_INSERT [dbo].[AuditLogs] OFF;

-- 4.23 Attachments (Credential Documents & Evidences)
SET IDENTITY_INSERT [dbo].[Attachments] ON;
INSERT INTO [dbo].[Attachments] (
    AttachmentId, OwnerType, OwnerId, Url, PublicId, FileName, MimeType, FileSize, DocType,
    UploadedByAccountId, UploadedAt, CreatedAt, IsDeleted
) VALUES
-- Jane Student (ID 5): FAA Certificate & Medical Assessment Class 1 (Expired)
(1, N'UserProfile', 5, N'https://res.cloudinary.com/hmzoqzkg/image/upload/v1790921559/cv7sdwunwrwwduovxzr4.pdf', N'cv7sdwunwrwwduovxzr4', N'01_FAA_Form_8710-1_Airman_Certificate_Application.pdf', N'application/pdf', 409501, N'License', 5, SYSUTCDATETIME(), SYSUTCDATETIME(), 0),
(2, N'UserProfile', 5, N'https://res.cloudinary.com/hmzoqzkg/image/upload/v1790921559/cv7sdwunwrwwduovxzr4.pdf', N'cv7sdwunwrwwduovxzr4_med', N'CAAV_Class1_Medical_Certificate_Expired.pdf', N'application/pdf', 256100, N'Medical', 5, '2025-08-15', '2025-08-15', 0),
-- Student 2 (ID 9): Expired CPL & Expired Medical
(3, N'UserProfile', 9, N'https://res.cloudinary.com/hmzoqzkg/image/upload/v1790921559/cv7sdwunwrwwduovxzr4.pdf', N'cv7sdwunwrwwduovxzr4_cpl2', N'CAAV_Commercial_Pilot_License_Expired_2026.pdf', N'application/pdf', 389200, N'License', 9, '2024-06-30', '2024-06-30', 0),
(4, N'UserProfile', 9, N'https://res.cloudinary.com/hmzoqzkg/image/upload/v1790921559/cv7sdwunwrwwduovxzr4.pdf', N'cv7sdwunwrwwduovxzr4_med2', N'CAAV_Class1_Medical_Expired_2026.pdf', N'application/pdf', 214500, N'Medical', 9, '2025-07-20', '2025-07-20', 0);
SET IDENTITY_INSERT [dbo].[Attachments] OFF;

-- ==============================================================================
-- 5. RE-ENABLE ALL CONSTRAINTS
-- ==============================================================================
DECLARE @sqlEnable NVARCHAR(MAX) = N'';
SELECT @sqlEnable += N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' WITH CHECK CHECK CONSTRAINT ALL; '
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.name != '__EFMigrationsHistory';
EXEC sp_executesql @sqlEnable;

PRINT 'ETR Master Demo Database deployed and seeded successfully!';
