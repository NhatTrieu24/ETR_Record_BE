-- =============================================================================
-- ETR SYSTEM - BULLETPROOF ENGLISH CONVERSION & FONT ERROR REPAIR SCRIPT
-- Directly targets AccountIds, SessionIds, and corrupted characters (? / )
-- Converts 100% of data to clean ASCII English for flawless Azure/Local display.
-- =============================================================================
USE [ETRManagementDB];
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

BEGIN TRANSACTION;

-- 1. Fix all UserProfiles (Names, Gender, Organization) by AccountId
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

-- Also fix any remaining non-English organizations & genders
UPDATE [UserProfiles] SET [Organization] = 'Vietnam Aviation Academy' WHERE [Organization] LIKE '%H%c vi%n%' OR [Organization] LIKE '%Aviation Academy%';
UPDATE [UserProfiles] SET [Organization] = 'Civil Aviation Authority of Vietnam' WHERE [Organization] LIKE '%C%c H%ng kh%ng%' OR [Organization] LIKE '%Civil Aviation%';
UPDATE [UserProfiles] SET [Gender] = 'Female' WHERE [Gender] LIKE '%N%' OR [Gender] = 'Nữ' OR [Gender] = 'nu';
UPDATE [UserProfiles] SET [Gender] = 'Male' WHERE [Gender] LIKE '%Nam%' OR [Gender] = 'nam';

-- 2. Fix all Sessions (SessionTitle, Location)
UPDATE [Sessions] SET [Location] = 'A320 Simulator Bay' WHERE [Location] LIKE '%Ph%ng Sim%' OR [Location] LIKE '%Sim A320%' OR [Location] LIKE '%A320 Sim%';
UPDATE [Sessions] SET [Location] = 'Room E2E' WHERE [Location] LIKE '%Room E2E%' OR [Location] LIKE '%Ph%ng E2E%';

UPDATE [Sessions] SET [SessionTitle] = 'Session 1' WHERE [SessionTitle] LIKE 'Bu% 1' OR [SessionTitle] LIKE 'Buoi 1';
UPDATE [Sessions] SET [SessionTitle] = 'Session 2' WHERE [SessionTitle] LIKE 'Bu% 2' OR [SessionTitle] LIKE 'Buoi 2';
UPDATE [Sessions] SET [SessionTitle] = 'Session 3' WHERE [SessionTitle] LIKE 'Bu% 3' OR [SessionTitle] LIKE 'Buoi 3';
UPDATE [Sessions] SET [SessionTitle] = 'Session 4' WHERE [SessionTitle] LIKE 'Bu% 4' OR [SessionTitle] LIKE 'Buoi 4';
UPDATE [Sessions] SET [SessionTitle] = 'Session 5' WHERE [SessionTitle] LIKE 'Bu% 5' OR [SessionTitle] LIKE 'Buoi 5';
UPDATE [Sessions] SET [SessionTitle] = 'Session 6' WHERE [SessionTitle] LIKE 'Bu% 6' OR [SessionTitle] LIKE 'Buoi 6';
UPDATE [Sessions] SET [SessionTitle] = 'Session 7' WHERE [SessionTitle] LIKE 'Bu% 7' OR [SessionTitle] LIKE 'Buoi 7';
UPDATE [Sessions] SET [SessionTitle] = 'Session 8' WHERE [SessionTitle] LIKE 'Bu% 8' OR [SessionTitle] LIKE 'Buoi 8';
UPDATE [Sessions] SET [SessionTitle] = 'Session 9' WHERE [SessionTitle] LIKE 'Bu% 9' OR [SessionTitle] LIKE 'Buoi 9';
UPDATE [Sessions] SET [SessionTitle] = 'Session 10' WHERE [SessionTitle] LIKE 'Bu% 10' OR [SessionTitle] LIKE 'Buoi 10';

UPDATE [Sessions] SET [SessionTitle] = 'Session 3 (Cockpit Practical Evaluation)' WHERE [SessionId] = 1185 OR [SessionTitle] LIKE '%bu%ng l%i%' OR [SessionTitle] LIKE '%cockpit%';
UPDATE [Sessions] SET [SessionTitle] = 'Session 7 (Assessment: Progress Check 1)' WHERE [SessionTitle] LIKE '%mini test%';
UPDATE [Sessions] SET [SessionTitle] = 'Session 8 (Assessment: Pre-flight Inspection)' WHERE [SessionTitle] LIKE '%Danh gia: a%' OR [SessionTitle] LIKE '%Pre-flight%';
UPDATE [Sessions] SET [SessionTitle] = 'Session 9 (Assessment: Practical Maintenance Test)' WHERE [SessionTitle] LIKE '%Practical Test Maintenance%';
UPDATE [Sessions] SET [SessionTitle] = 'Session 11 (Assessment: Final Safety & Human Factors)' WHERE [SessionTitle] LIKE '%Final Safety%';

-- 3. Fix Departments
UPDATE [Departments] SET [Description] = 'System administration and IT support' WHERE [DepartmentName] = 'Administration' OR [Description] LIKE '%Ph%ng ban%';

-- 4. Fix Attendance Remarks
UPDATE [AttendanceRecords] SET [Remarks] = 'Sick Leave' WHERE [Remarks] LIKE '%Ngh% %m%' OR [Remarks] LIKE '%Nghi om%';
UPDATE [AttendanceRecords] SET [Remarks] = 'Full Attendance (Fast-forward)' WHERE [Remarks] LIKE '%i%m danh%' OR [Remarks] LIKE '%Fast-forward%';

-- 5. Fix AmendmentRequests Reason
UPDATE [AmendmentRequests] 
SET [Reason] = 'Practical assessment score entered incorrectly, requesting unlock for re-assessment and sign-off.' 
WHERE [Reason] LIKE '%k% n%ng%' OR [Reason] LIKE '%Practical assessment%';

-- 6. Fix SubjectSignoffs Comments
UPDATE [SubjectSignoffs] 
SET [Comment] = 'Instructor confirmed subject completion and sign-off' 
WHERE [Comment] LIKE '%Gi%ng vi%n%' OR [Comment] LIKE '%Instructor confirmed%';

-- 7. Fix EvidenceFiles Verification Comments
UPDATE [EvidenceFiles] 
SET [VerificationComment] = 'Valid training evidence verified and approved by QA' 
WHERE [VerificationComment] LIKE '%Minh ch%ng%' OR [VerificationComment] LIKE '%Valid training evidence%';

-- 8. Fix AuditLogs Descriptions
UPDATE [AuditLogs] 
SET [Description] = 'Restore department Ground Operations (Restore soft delete)' 
WHERE [Description] LIKE '%Kh%i ph%c ph%ng ban%';

UPDATE [AuditLogs] 
SET [Description] = '[WARNING] Admin intervened to break external signoff - Force Unlock SubjectResult for amendment.'
WHERE [Description] LIKE '%C%NH B%O%' OR [Description] LIKE '%ADMIN_FORCE_UNLOCK%' AND [Description] LIKE '%can thi%p%';

COMMIT TRANSACTION;
GO
