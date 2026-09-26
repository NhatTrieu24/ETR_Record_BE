-- =============================================================================
-- ETR SYSTEM - ULTIMATE DATABASE CLEANING & ENGLISH NORMALIZATION SCRIPT
-- Fixes all font corruption, mojibake (Ä/Ã/Æ), question marks (?), and test data.
-- Run this script in Azure Portal Query Editor or SSMS connected to Azure SQL DB.
-- =============================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

BEGIN TRANSACTION;

-- 1. FIX ALL USER PROFILES (Full Names, Gender, Organization)
UPDATE [UserProfiles] SET [FullName] = 'System Admin', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 1;
UPDATE [UserProfiles] SET [FullName] = 'Senior Instructor', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 2;
UPDATE [UserProfiles] SET [FullName] = 'QA Specialist', [Gender] = 'Female', [Organization] = 'ETR Aviation' WHERE [AccountId] = 3;
UPDATE [UserProfiles] SET [FullName] = 'Training Manager', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 4;
UPDATE [UserProfiles] SET [FullName] = 'Jane Student', [Gender] = 'Female', [Organization] = 'ETR Aviation' WHERE [AccountId] = 5;
UPDATE [UserProfiles] SET [FullName] = 'Management Viewer', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 6;
UPDATE [UserProfiles] SET [FullName] = 'Academic Staff', [Gender] = 'Female', [Organization] = 'ETR Aviation' WHERE [AccountId] = 7;
UPDATE [UserProfiles] SET [FullName] = 'Audit Staff', [Gender] = 'Male', [Organization] = 'Civil Aviation Authority of Vietnam' WHERE [AccountId] = 8;

UPDATE [UserProfiles] SET [FullName] = 'Dang Gia Huy', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 47;
UPDATE [UserProfiles] SET [FullName] = 'Tran Trong Nhan', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 48;
UPDATE [UserProfiles] SET [FullName] = 'Nguyen Huu Trieu', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 49;
UPDATE [UserProfiles] SET [FullName] = 'Nguyen Van Trieu (Flight Instructor)', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 50;
UPDATE [UserProfiles] SET [FullName] = 'Nguyen Van An', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 51;
UPDATE [UserProfiles] SET [FullName] = 'Tran Thi Binh', [Gender] = 'Female', [Organization] = 'ETR Aviation' WHERE [AccountId] = 52;
UPDATE [UserProfiles] SET [FullName] = 'Le Hoang Cuong', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 53;
UPDATE [UserProfiles] SET [FullName] = 'Pham Minh Duc', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 54;
UPDATE [UserProfiles] SET [FullName] = 'Vu Hai Dang', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 55;
UPDATE [UserProfiles] SET [FullName] = 'Dang Thuy Duong', [Gender] = 'Female', [Organization] = 'ETR Aviation' WHERE [AccountId] = 56;
UPDATE [UserProfiles] SET [FullName] = 'Bui Quoc Huy', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 57;
UPDATE [UserProfiles] SET [FullName] = 'Hoang Yen Linh', [Gender] = 'Female', [Organization] = 'ETR Aviation' WHERE [AccountId] = 58;
UPDATE [UserProfiles] SET [FullName] = 'Ngo Gia Khiem', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 59;
UPDATE [UserProfiles] SET [FullName] = 'Do Thu Ngan', [Gender] = 'Female', [Organization] = 'ETR Aviation' WHERE [AccountId] = 60;
UPDATE [UserProfiles] SET [FullName] = 'Duong Bao Nam', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 61;
UPDATE [UserProfiles] SET [FullName] = 'Vo Phuong Oanh', [Gender] = 'Female', [Organization] = 'ETR Aviation' WHERE [AccountId] = 62;
UPDATE [UserProfiles] SET [FullName] = 'Do Thao Vy', [Gender] = 'Female', [Organization] = 'ETR Aviation' WHERE [AccountId] = 63;
UPDATE [UserProfiles] SET [FullName] = 'Ly Van Cuong', [Gender] = 'Male', [Organization] = 'ETR Aviation' WHERE [AccountId] = 64;

-- Clean any remaining Gender & Organization strings
UPDATE [UserProfiles] SET [Gender] = 'Female' WHERE [Gender] IN ('Nữ', 'Ná»¯', 'N?', 'nu', 'Female') OR [Gender] LIKE 'N%';
UPDATE [UserProfiles] SET [Gender] = 'Male' WHERE [Gender] IN ('Nam', 'nam', 'Male') OR [Gender] LIKE 'Nam%';
UPDATE [UserProfiles] SET [Organization] = 'ETR Aviation' WHERE [Organization] IS NULL OR [Organization] = '';
UPDATE [UserProfiles] SET [Organization] = 'Vietnam Aviation Academy' WHERE [Organization] LIKE '%H%c vi%n%' OR [Organization] LIKE '%Aviation Academy%';
UPDATE [UserProfiles] SET [Organization] = 'Civil Aviation Authority of Vietnam' WHERE [Organization] LIKE '%C%c H%ng kh%ng%' OR [Organization] LIKE '%Civil Aviation%';

-- 2. FIX STRANGE COURSES (TestCouse269, etc.)
UPDATE [Courses] SET [CourseName] = 'Flight Dynamics & Simulation', [CourseCode] = 'AV-SIM-269' WHERE [CourseName] LIKE '%TestCouse269%' OR [CourseCode] LIKE '%TestCouse269%';
UPDATE [Courses] SET [CourseName] = 'Commercial Pilot Ground School', [CourseCode] = 'AV-CPL-17' WHERE [CourseName] LIKE '%test course%';
UPDATE [Courses] SET [CourseName] = 'Flight Navigation & Meteorology', [CourseCode] = 'CPL-NAV-01' WHERE [CourseName] LIKE '%TestCouseCT1%';
UPDATE [Courses] SET [CourseName] = 'Advanced Flight Systems (ATPL)', [CourseCode] = 'ATPL-SYS-01' WHERE [CourseName] LIKE '%TestCouse 21 9%' OR [CourseCode] = '21-9T';
UPDATE [Courses] SET [CourseName] = 'Flight Instructor Rating (FIR)', [CourseCode] = 'FIR-INSTR-01' WHERE [CourseName] LIKE '%Demo-course922%' OR [CourseCode] LIKE '%Flight Instructor Rating%';
UPDATE [Courses] SET [CourseName] = 'Crew Resource Management (CRM)', [CourseCode] = 'CRM-OPS-01' WHERE [CourseName] LIKE '%Demo-course1022%' OR [CourseCode] = 'COURSE-1022';
UPDATE [Courses] SET [CourseName] = 'Basic Flight Operations 1', [CourseCode] = 'FPT-FLY-101' WHERE [CourseName] = 'FPT-FLY';
UPDATE [Courses] SET [CourseName] = 'Basic Flight Operations 2', [CourseCode] = 'FPT-FLY-102' WHERE [CourseName] = 'FPT-FLY 2' OR [CourseCode] = 'AV-MNT-5422';

-- 3. FIX STRANGE CLASSES & LOCATIONS
UPDATE [Classes] SET [ClassName] = 'Flight Simulation Cohort 269', [ClassCode] = 'SIM-C269', [Location] = 'A320 Simulator Bay' WHERE [ClassCode] LIKE '%TESTCLASS269%' OR [ClassName] LIKE '%CLASS269%';
UPDATE [Classes] SET [ClassName] = 'Flight Instructor Batch 229', [ClassCode] = 'FIR-C229', [Location] = 'A320 Simulator Bay' WHERE [ClassName] LIKE '%Flight Instructor Batch 229%';
UPDATE [Classes] SET [ClassName] = 'Crew Resource Management Batch 1', [ClassCode] = 'CRM-C01', [Location] = 'A320 Simulator Bay' WHERE [ClassName] LIKE '%Crew Resource Management Batch 1%';
UPDATE [Classes] SET [ClassName] = 'CPL Ground School Batch 219', [ClassCode] = 'CPL-C219', [Location] = 'A320 Simulator Bay' WHERE [ClassName] LIKE '%Batch 219%';
UPDATE [Classes] SET [ClassName] = 'CPL Ground School Batch 220', [ClassCode] = 'CPL-C220', [Location] = 'A320 Simulator Bay' WHERE [ClassName] LIKE '%Batch 220%';

-- Convert all location encodings across Classes & Sessions
UPDATE [Classes] SET [Location] = 'A320 Simulator Bay' WHERE [Location] LIKE '%Ph%ng Sim%' OR [Location] LIKE '%Sim A320%' OR [Location] LIKE '%A320 Sim%' OR [Location] LIKE '%PhÃ²ng%';
UPDATE [Classes] SET [Location] = 'Flight Training Hangar' WHERE [Location] LIKE '%Hangar%' OR [Location] IS NULL;
UPDATE [Sessions] SET [Location] = 'A320 Simulator Bay' WHERE [Location] LIKE '%Ph%ng Sim%' OR [Location] LIKE '%Sim A320%' OR [Location] LIKE '%A320 Sim%' OR [Location] LIKE '%PhÃ²ng%';
UPDATE [Sessions] SET [Location] = 'Room E2E' WHERE [Location] LIKE '%Room E2E%' OR [Location] LIKE '%Ph%ng E2E%';
UPDATE [Sessions] SET [Location] = 'Flight Training Hangar' WHERE [Location] IS NULL;

-- 4. FIX SESSIONS (SessionTitle)
UPDATE [Sessions] SET [SessionTitle] = 'Session 1' WHERE [SessionTitle] LIKE 'Bu% 1' OR [SessionTitle] LIKE 'Buoi 1' OR [SessionTitle] = 'Buổi 1';
UPDATE [Sessions] SET [SessionTitle] = 'Session 2' WHERE [SessionTitle] LIKE 'Bu% 2' OR [SessionTitle] LIKE 'Buoi 2' OR [SessionTitle] = 'Buổi 2';
UPDATE [Sessions] SET [SessionTitle] = 'Session 3' WHERE [SessionTitle] LIKE 'Bu% 3' OR [SessionTitle] LIKE 'Buoi 3' OR [SessionTitle] = 'Buổi 3';
UPDATE [Sessions] SET [SessionTitle] = 'Session 4' WHERE [SessionTitle] LIKE 'Bu% 4' OR [SessionTitle] LIKE 'Buoi 4' OR [SessionTitle] = 'Buổi 4';
UPDATE [Sessions] SET [SessionTitle] = 'Session 5' WHERE [SessionTitle] LIKE 'Bu% 5' OR [SessionTitle] LIKE 'Buoi 5' OR [SessionTitle] = 'Buổi 5';
UPDATE [Sessions] SET [SessionTitle] = 'Session 6' WHERE [SessionTitle] LIKE 'Bu% 6' OR [SessionTitle] LIKE 'Buoi 6' OR [SessionTitle] = 'Buổi 6';
UPDATE [Sessions] SET [SessionTitle] = 'Session 7' WHERE [SessionTitle] LIKE 'Bu% 7' OR [SessionTitle] LIKE 'Buoi 7' OR [SessionTitle] = 'Buổi 7';
UPDATE [Sessions] SET [SessionTitle] = 'Session 8' WHERE [SessionTitle] LIKE 'Bu% 8' OR [SessionTitle] LIKE 'Buoi 8' OR [SessionTitle] = 'Buổi 8';
UPDATE [Sessions] SET [SessionTitle] = 'Session 9' WHERE [SessionTitle] LIKE 'Bu% 9' OR [SessionTitle] LIKE 'Buoi 9' OR [SessionTitle] = 'Buổi 9';
UPDATE [Sessions] SET [SessionTitle] = 'Session 10' WHERE [SessionTitle] LIKE 'Bu% 10' OR [SessionTitle] LIKE 'Buoi 10' OR [SessionTitle] = 'Buổi 10';

UPDATE [Sessions] SET [SessionTitle] = 'Session 3 (Cockpit Practical Evaluation)' WHERE [SessionTitle] LIKE '%bu%ng l%i%' OR [SessionTitle] LIKE '%cockpit%' OR [SessionTitle] LIKE '%thực hành buồng lái%';
UPDATE [Sessions] SET [SessionTitle] = 'Session 7 (Assessment: Progress Check 1)' WHERE [SessionTitle] LIKE '%mini test%';
UPDATE [Sessions] SET [SessionTitle] = 'Session 8 (Assessment: Pre-flight Inspection)' WHERE [SessionTitle] LIKE '%Danh gia: a%' OR [SessionTitle] LIKE '%Pre-flight%';
UPDATE [Sessions] SET [SessionTitle] = 'Session 9 (Assessment: Practical Maintenance Test)' WHERE [SessionTitle] LIKE '%Practical Test Maintenance%';
UPDATE [Sessions] SET [SessionTitle] = 'Session 11 (Assessment: Final Safety & Human Factors)' WHERE [SessionTitle] LIKE '%Final Safety%';

-- 5. FIX DEPARTMENTS
UPDATE [Departments] SET [Description] = 'System administration and IT support' WHERE [DepartmentName] = 'Administration' OR [Description] LIKE '%Ph%ng ban%';

-- 6. FIX ATTENDANCE REMARKS
UPDATE [AttendanceRecords] SET [Remarks] = 'Sick Leave' WHERE [Remarks] LIKE '%Ngh% %m%' OR [Remarks] LIKE '%Nghi om%' OR [Remarks] LIKE '%ốm%';
UPDATE [AttendanceRecords] SET [Remarks] = 'Full Attendance (Fast-forward)' WHERE [Remarks] LIKE '%i%m danh%' OR [Remarks] LIKE '%Fast-forward%' OR [Remarks] LIKE '%Điểm danh%';

-- 7. FIX AMENDMENT REQUESTS
UPDATE [AmendmentRequests] 
SET [Reason] = 'Practical assessment score entered incorrectly, requesting unlock for re-assessment and sign-off.' 
WHERE [Reason] LIKE '%k% n%ng%' OR [Reason] LIKE '%Practical assessment%' OR [Reason] LIKE '%kỹ năng%';

-- 8. FIX SUBJECT SIGNOFFS & EVIDENCE
UPDATE [SubjectSignoffs] 
SET [Comment] = 'Instructor confirmed subject completion and sign-off' 
WHERE [Comment] LIKE '%Gi%ng vi%n%' OR [Comment] LIKE '%Instructor confirmed%' OR [Comment] LIKE '%Giảng viên%';

UPDATE [EvidenceFiles] 
SET [VerificationComment] = 'Valid training evidence verified and approved by QA' 
WHERE [VerificationComment] LIKE '%Minh ch%ng%' OR [VerificationComment] LIKE '%Valid training evidence%' OR [VerificationComment] LIKE '%Minh chứng%';

-- 9. FIX AUDIT LOGS
UPDATE [AuditLogs] 
SET [Description] = 'Restore department Ground Operations (Restore soft delete)' 
WHERE [Description] LIKE '%Kh%i ph%c ph%ng ban%' OR [Description] LIKE '%Khôi phục%';

UPDATE [AuditLogs] 
SET [Description] = '[WARNING] Admin intervened to break external signoff - Force Unlock SubjectResult for amendment.'
WHERE [Description] LIKE '%C%NH B%O%' OR [Description] LIKE '%ADMIN_FORCE_UNLOCK%' AND ([Description] LIKE '%can thi%p%' OR [Description] LIKE '%ký bởi%');

-- 10. CLEAN ASSESSMENT NAMES
UPDATE [Assessments] SET [ComponentName] = 'Flight Regulations Assessment' WHERE [ComponentName] LIKE '%E2E-PROBE%' OR [ComponentName] LIKE '%Kiểm tra tạo%';
UPDATE [Assessments] SET [ComponentName] = 'Pre-flight Inspection Assessment' WHERE [ComponentName] = 'a';
UPDATE [Assessments] SET [ComponentName] = 'Radio Communications Check' WHERE [ComponentName] = 'v';
UPDATE [Assessments] SET [ComponentName] = 'Simulator Navigation Test' WHERE [ComponentName] = 'b';
UPDATE [Assessments] SET [ComponentName] = 'Progress Check 1' WHERE [ComponentName] LIKE '%mini test%';
UPDATE [Assessments] SET [ComponentName] = 'Progress Check 2' WHERE [ComponentName] LIKE '%mini tesy%';
UPDATE [Assessments] SET [ComponentName] = 'Final Qualification Exam' WHERE [ComponentName] = 'final';

COMMIT TRANSACTION;
GO
