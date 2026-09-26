-- =============================================================================
-- ETR SYSTEM - DATA NORMALIZATION & COMPLIANCE ENHANCEMENT SCRIPT
-- Cleans strange/test names, normalizes ETR InProgress -> Draft,
-- and enriches Verified queue for flawless live demonstration.
-- =============================================================================
USE [ETRManagementDB];
GO

BEGIN TRANSACTION;

-- 1. Chuẩn hóa tên học viên & email / username lạ
UPDATE [UserProfiles] SET [FullName] = N'Jane Student', [Email] = N'student@etr.com' WHERE [FullName] = N'Jane Student edit' OR [Email] = N'student@etr.com';
UPDATE [Accounts] SET [Username] = N'student@etr.com' WHERE [Username] = N'student@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Tran Trong Nhan', [Email] = N'nhan.tran@etr.com' WHERE [FullName] = N'Test' OR [Email] = N'Nha@etr.com';
UPDATE [Accounts] SET [Username] = N'nhan.tran@etr.com' WHERE [Username] = N'Nha@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Nguyen Huu Trieu', [Email] = N'trieu.student@etr.com' WHERE [FullName] = N'Trieu Student' OR [Email] = N'trieustudent@gmail.com';
UPDATE [Accounts] SET [Username] = N'trieu.student@etr.com' WHERE [Username] = N'trieustudent@gmail.com';
UPDATE [UserProfiles] SET [FullName] = N'Nguyen Van Trieu (Flight Instructor)', [Email] = N'trieu.instructor@etr.com' WHERE [FullName] = N'trieu Instruc' OR [Email] = N'trieuinstruc@gmail.com';
UPDATE [Accounts] SET [Username] = N'trieu.instructor@etr.com' WHERE [Username] = N'trieuinstruc@gmail.com';
UPDATE [UserProfiles] SET [FullName] = N'Dang Gia Huy', [Email] = N'giahuy@etr.com' WHERE [FullName] = N'Gia Huy' OR [Email] = N'huy0312@gmail.com';
UPDATE [Accounts] SET [Username] = N'giahuy@etr.com' WHERE [Username] = N'huy0312@gmail.com';
UPDATE [UserProfiles] SET [FullName] = N'Nguyen Van An', [Email] = N'an.nguyen@etr.com' WHERE [FullName] = N'Nguyen Van Mot 893607' OR [Email] = N'e2e.stu1.893607@etr.com';
UPDATE [Accounts] SET [Username] = N'an.nguyen@etr.com' WHERE [Username] = N'e2e.stu1.893607@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Tran Thi Binh', [Email] = N'binh.tran@etr.com' WHERE [FullName] = N'Tran Thi Hai 893607' OR [Email] = N'e2e.stu2.893607@etr.com';
UPDATE [Accounts] SET [Username] = N'binh.tran@etr.com' WHERE [Username] = N'e2e.stu2.893607@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Le Hoang Cuong', [Email] = N'cuong.le@etr.com' WHERE [FullName] = N'Nguyen Van Mot 509955' OR [Email] = N'e2e.stu1.509955@etr.com';
UPDATE [Accounts] SET [Username] = N'cuong.le@etr.com' WHERE [Username] = N'e2e.stu1.509955@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Pham Minh Duc', [Email] = N'duc.pham@etr.com' WHERE [FullName] = N'Tran Thi Hai 509955' OR [Email] = N'e2e.stu2.509955@etr.com';
UPDATE [Accounts] SET [Username] = N'duc.pham@etr.com' WHERE [Username] = N'e2e.stu2.509955@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Vu Hai Dang', [Email] = N'dang.vu@etr.com' WHERE [FullName] = N'Nguyen Van Mot 872294' OR [Email] = N'e2e.stu1.872294@etr.com';
UPDATE [Accounts] SET [Username] = N'dang.vu@etr.com' WHERE [Username] = N'e2e.stu1.872294@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Dang Thuy Duong', [Email] = N'duong.dang@etr.com' WHERE [FullName] = N'Tran Thi Hai 872294' OR [Email] = N'e2e.stu2.872294@etr.com';
UPDATE [Accounts] SET [Username] = N'duong.dang@etr.com' WHERE [Username] = N'e2e.stu2.872294@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Bui Quoc Huy', [Email] = N'huy.bui@etr.com' WHERE [FullName] = N'Nguyen Van Mot 223561' OR [Email] = N'e2e.stu1.223561@etr.com';
UPDATE [Accounts] SET [Username] = N'huy.bui@etr.com' WHERE [Username] = N'e2e.stu1.223561@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Hoang Yen Linh', [Email] = N'linh.hoang@etr.com' WHERE [FullName] = N'Tran Thi Hai 223561' OR [Email] = N'e2e.stu2.223561@etr.com';
UPDATE [Accounts] SET [Username] = N'linh.hoang@etr.com' WHERE [Username] = N'e2e.stu2.223561@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Ngo Gia Khiem', [Email] = N'khiem.ngo@etr.com' WHERE [FullName] = N'Nguyen Van Mot 360083' OR [Email] = N'e2e.stu1.360083@etr.com';
UPDATE [Accounts] SET [Username] = N'khiem.ngo@etr.com' WHERE [Username] = N'e2e.stu1.360083@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Do Thu Ngan', [Email] = N'ngan.do@etr.com' WHERE [FullName] = N'Tran Thi Hai 360083' OR [Email] = N'e2e.stu2.360083@etr.com';
UPDATE [Accounts] SET [Username] = N'ngan.do@etr.com' WHERE [Username] = N'e2e.stu2.360083@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Duong Bao Nam', [Email] = N'nam.duong@etr.com' WHERE [FullName] = N'Nguyen Van Mot 684755' OR [Email] = N'e2e.stu1.684755@etr.com';
UPDATE [Accounts] SET [Username] = N'nam.duong@etr.com' WHERE [Username] = N'e2e.stu1.684755@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Vo Phuong Oanh', [Email] = N'oanh.vo@etr.com' WHERE [FullName] = N'Tran Thi Hai 684755' OR [Email] = N'e2e.stu2.684755@etr.com';
UPDATE [Accounts] SET [Username] = N'oanh.vo@etr.com' WHERE [Username] = N'e2e.stu2.684755@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Do Thao Vy', [Email] = N'vy.do@etr.com' WHERE [FullName] = N'excel temp' OR [Email] = N'Exceltest@etr.com';
UPDATE [Accounts] SET [Username] = N'vy.do@etr.com' WHERE [Username] = N'Exceltest@etr.com';
UPDATE [UserProfiles] SET [FullName] = N'Ly Van Cuong', [Email] = N'cuong.ly@etr.com' WHERE [FullName] = N'Lý Văn C' OR [Email] = N'student111@etr.com';
UPDATE [Accounts] SET [Username] = N'cuong.ly@etr.com' WHERE [Username] = N'student111@etr.com';

-- 2. Chuẩn hóa tên và mã khóa học lạ
UPDATE [Courses] SET [CourseName] = N'Commercial Pilot Ground School', [CourseCode] = N'AV-CPL-17' WHERE [CourseName] = N'test course';
UPDATE [Courses] SET [CourseName] = N'Flight Navigation & Meteorology', [CourseCode] = N'CPL-NAV-01' WHERE [CourseName] = N'TestCouseCT1';
UPDATE [Courses] SET [CourseName] = N'Advanced Flight Systems (ATPL)', [CourseCode] = N'ATPL-SYS-01' WHERE [CourseName] = N'TestCouse 21 9';
UPDATE [Courses] SET [CourseName] = N'Flight Instructor Rating (FIR)', [CourseCode] = N'FIR-INSTR-01' WHERE [CourseName] = N'Demo-course922';
UPDATE [Courses] SET [CourseName] = N'Crew Resource Management (CRM)', [CourseCode] = N'CRM-OPS-01' WHERE [CourseName] = N'Demo-course1022';
UPDATE [Courses] SET [CourseName] = N'Basic Flight Operations 1', [CourseCode] = N'FPT-FLY-101' WHERE [CourseName] = N'FPT-FLY';
UPDATE [Courses] SET [CourseName] = N'Basic Flight Operations 2', [CourseCode] = N'FPT-FLY-102' WHERE [CourseName] = N'FPT-FLY 2';

-- 3. Chuẩn hóa tên môn học lạ & sửa lỗi chính tả
UPDATE [Subjects] SET [SubjectName] = N'Aviation Security Management', [SubjectCode] = N'SJ-SEC' WHERE [SubjectName] = N'sercurity';
UPDATE [Subjects] SET [SubjectName] = N'Radio Navigation & Flight Instruments', [SubjectCode] = N'SJ-NAV' WHERE [SubjectName] = N'test subject a1';

-- 4. Chuẩn hóa tên và mã lớp học lạ (Lop Import, TESTCLASS)
UPDATE [Classes] SET [ClassName] = N'Aviation Security Cohort 2026-A', [ClassCode] = N'SEC-0312' WHERE [ClassName] = N'SEC-0312 - 18/8';
UPDATE [Classes] SET [ClassName] = N'Flight Operations Cohort 01', [ClassCode] = N'FLY-C01' WHERE [ClassName] = N'FPT-FLY 1';
UPDATE [Classes] SET [ClassName] = N'Flight Operations Cohort 02', [ClassCode] = N'FLY-C02' WHERE [ClassName] = N'FPT FLY2 18/8';
UPDATE [Classes] SET [ClassName] = N'Flight Simulator Cohort Alpha', [ClassCode] = N'SIM-C01' WHERE [ClassName] = N'test class s1';
UPDATE [Classes] SET [ClassName] = N'AMT Ground Cohort 2026-A', [ClassCode] = N'AMT-C01' WHERE [ClassName] = N'TESTCLASS001';
UPDATE [Classes] SET [ClassName] = N'AMT Ground Cohort 2026-B', [ClassCode] = N'AMT-C02' WHERE [ClassName] = N'TESTCLASS002';
UPDATE [Classes] SET [ClassName] = N'Flight Crew Cohort 31', [ClassCode] = N'FCC-031' WHERE [ClassName] = N'Lop Import E2E 893607';
UPDATE [Classes] SET [ClassName] = N'Flight Crew Cohort 32', [ClassCode] = N'FCC-032' WHERE [ClassName] = N'Lop Import E2E 509955';
UPDATE [Classes] SET [ClassName] = N'Flight Crew Cohort 33', [ClassCode] = N'FCC-033' WHERE [ClassName] = N'Lop Import E2E 872294';
UPDATE [Classes] SET [ClassName] = N'Flight Crew Cohort 34', [ClassCode] = N'FCC-034' WHERE [ClassName] = N'Lop Import E2E 223561';
UPDATE [Classes] SET [ClassName] = N'Flight Crew Cohort 35', [ClassCode] = N'FCC-035' WHERE [ClassName] = N'Lop Import E2E 360083';
UPDATE [Classes] SET [ClassName] = N'Flight Crew Cohort 36', [ClassCode] = N'FCC-036' WHERE [ClassName] = N'Lop Import E2E 684755';
UPDATE [Classes] SET [ClassName] = N'CPL Ground School Batch 219', [ClassCode] = N'CPL-C01' WHERE [ClassName] = N'TESTCLASS219';
UPDATE [Classes] SET [ClassName] = N'CPL Ground School Batch 220', [ClassCode] = N'CPL-C02' WHERE [ClassName] = N'TESTCLASS220';
UPDATE [Classes] SET [ClassName] = N'Flight Instructor Batch 229', [ClassCode] = N'FIR-C01' WHERE [ClassName] = N'TESTCLASS229';
UPDATE [Classes] SET [ClassName] = N'Crew Resource Management Batch 1', [ClassCode] = N'CRM-C01' WHERE [ClassName] = N'TESTCLASS-1022-1';

-- 5. Chuẩn hóa tên bài kiểm tra lạ (mini test, probe, a, v, b)
UPDATE [Assessments] SET [AssessmentName] = N'Progress Check 1' WHERE [AssessmentName] = N'mini test';
UPDATE [Assessments] SET [AssessmentName] = N'Flight Regulations Assessment' WHERE [AssessmentName] = N'[E2E-PROBE] Kiểm tra tạo 1790019197220';
UPDATE [Assessments] SET [AssessmentName] = N'Pre-flight Inspection Assessment' WHERE [AssessmentName] = N'a';
UPDATE [Assessments] SET [AssessmentName] = N'Radio Communications Check' WHERE [AssessmentName] = N'v';
UPDATE [Assessments] SET [AssessmentName] = N'Simulator Navigation Test' WHERE [AssessmentName] = N'b';
UPDATE [Assessments] SET [AssessmentName] = N'Progress Check 2' WHERE [AssessmentName] = N'mini tesy';
UPDATE [Assessments] SET [AssessmentName] = N'Final Qualification Exam' WHERE [AssessmentName] = N'final';

-- 6. Chuẩn hóa trạng thái ETR: Đổi InProgress thành Draft theo đúng chuẩn SRS/Report 7
UPDATE [ETRCourseRecords] SET [Status] = N'Draft' WHERE [Status] = N'InProgress';

-- 7. Chuẩn bị thêm 4 hồ sơ ETR ở trạng thái 'Verified' để Training Manager demo duyệt thoải mái
UPDATE [ETRCourseRecords] 
SET [Status] = N'Verified', [VerifiedAt] = '2026-09-25 10:00:00.000', [IsLocked] = 0
WHERE [ETRCourseRecordId] IN (19, 21, 23, 25) AND [Status] = N'Submitted';

-- 8. Đảm bảo toàn bộ tài khoản active đều có IsActive = 1 để đăng nhập không lỗi
UPDATE [Accounts] SET [IsActive] = 1 WHERE [Status] = N'Active';

COMMIT TRANSACTION;
GO

PRINT 'Data normalization completed successfully!';
GO
