-- =====================================================================
-- SEED / UPDATE SAMPLE DATA FOR EXPIRING & EXPIRED CERTIFICATES (ETR)
-- Target Database: ETRManagementDB(32GB) on Azure SQL
-- =====================================================================
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

DECLARE @Now DATETIME2 = GETUTCDATE();
PRINT 'Current UTC Time: ' + CONVERT(VARCHAR(30), @Now, 126);

-- 1. Đảm bảo ValidityMonths chuẩn cho các khóa học chính
UPDATE [Courses] SET [ValidityMonths] = 24 WHERE [CourseCode] = 'AMT-101';
UPDATE [Courses] SET [ValidityMonths] = 12 WHERE [CourseCode] = 'B737-TR';
UPDATE [Courses] SET [ValidityMonths] = 24 WHERE [CourseCode] = 'A320-FAM';
UPDATE [Courses] SET [ValidityMonths] = 36 WHERE [CourseCode] = 'ENG-101';
UPDATE [Courses] SET [ValidityMonths] = 12 WHERE [CourseCode] = 'SMS-101';
UPDATE [Courses] SET [ValidityMonths] = 24 WHERE [CourseCode] = 'HF-101';
UPDATE [Courses] SET [ValidityMonths] = 12 WHERE [CourseCode] = 'A350-TR';
UPDATE [Courses] SET [ValidityMonths] = 12 WHERE [CourseCode] = 'B787-TR';
UPDATE [Courses] SET [ValidityMonths] = 12 WHERE [CourseCode] = 'DGR-101';
UPDATE [Courses] SET [ValidityMonths] = 24 WHERE [CourseCode] = 'SEC-101';

-- 2. Cập nhật các bản ghi Completed ETR cụ thể với các mốc thời gian rõ ràng

-- -------------------------------------------------------------
-- [NHÓM 1: SẮP HẾT HẠN - EXPIRING SOON (Trong ngưỡng 30 ngày)]
-- -------------------------------------------------------------
-- ETR #1: student@etr.com | Khóa HF-101 -> Hạn còn 7 ngày
UPDATE [ETRCourseRecords]
SET [Status] = 'Completed',
    [IsLocked] = 1,
    [ExpiryDate] = DATEADD(day, 7, @Now),
    [IssuedDate] = DATEADD(month, -24, DATEADD(day, 7, @Now)),
    [CompletedAt] = DATEADD(month, -24, DATEADD(day, 7, @Now))
WHERE [ETRCourseRecordId] = 1;

-- ETR #6: student2@etr.com | Khóa B737-TR -> Hạn còn 12 ngày
UPDATE [ETRCourseRecords]
SET [Status] = 'Completed',
    [IsLocked] = 1,
    [ExpiryDate] = DATEADD(day, 12, @Now),
    [IssuedDate] = DATEADD(month, -12, DATEADD(day, 12, @Now)),
    [CompletedAt] = DATEADD(month, -12, DATEADD(day, 12, @Now))
WHERE [ETRCourseRecordId] = 6;

-- ETR #10: student4@etr.com | Khóa A320-FAM -> Hạn còn 20 ngày
UPDATE [ETRCourseRecords]
SET [Status] = 'Completed',
    [IsLocked] = 1,
    [ExpiryDate] = DATEADD(day, 20, @Now),
    [IssuedDate] = DATEADD(month, -24, DATEADD(day, 20, @Now)),
    [CompletedAt] = DATEADD(month, -24, DATEADD(day, 20, @Now))
WHERE [ETRCourseRecordId] = 10;

-- ETR #18: student7@etr.com | Khóa AMT-101 -> Hạn còn 5 ngày
UPDATE [ETRCourseRecords]
SET [Status] = 'Completed',
    [IsLocked] = 1,
    [ExpiryDate] = DATEADD(day, 5, @Now),
    [IssuedDate] = DATEADD(month, -24, DATEADD(day, 5, @Now)),
    [CompletedAt] = DATEADD(month, -24, DATEADD(day, 5, @Now))
WHERE [ETRCourseRecordId] = 18;

-- ETR #22: student8@etr.com | Khóa B787-TR -> Hạn còn 18 ngày
UPDATE [ETRCourseRecords]
SET [Status] = 'Completed',
    [IsLocked] = 1,
    [ExpiryDate] = DATEADD(day, 18, @Now),
    [IssuedDate] = DATEADD(month, -12, DATEADD(day, 18, @Now)),
    [CompletedAt] = DATEADD(month, -12, DATEADD(day, 18, @Now))
WHERE [ETRCourseRecordId] = 22;

-- ETR #34: student12@etr.com | Khóa SMS-101 -> Hạn còn 14 ngày
UPDATE [ETRCourseRecords]
SET [Status] = 'Completed',
    [IsLocked] = 1,
    [ExpiryDate] = DATEADD(day, 14, @Now),
    [IssuedDate] = DATEADD(month, -12, DATEADD(day, 14, @Now)),
    [CompletedAt] = DATEADD(month, -12, DATEADD(day, 14, @Now))
WHERE [ETRCourseRecordId] = 34;

-- -------------------------------------------------------------
-- [NHÓM 2: ĐÃ HẾT HẠN - EXPIRED]
-- -------------------------------------------------------------
-- ETR #3: student@etr.com | Khóa B787-TR -> Hết hạn 15 ngày trước
UPDATE [ETRCourseRecords]
SET [Status] = 'Completed',
    [IsLocked] = 1,
    [ExpiryDate] = DATEADD(day, -15, @Now),
    [IssuedDate] = DATEADD(month, -12, DATEADD(day, -15, @Now)),
    [CompletedAt] = DATEADD(month, -12, DATEADD(day, -15, @Now))
WHERE [ETRCourseRecordId] = 3;

-- ETR #9: student3@etr.com | Khóa A320-FAM -> Hết hạn 5 ngày trước
UPDATE [ETRCourseRecords]
SET [Status] = 'Completed',
    [IsLocked] = 1,
    [ExpiryDate] = DATEADD(day, -5, @Now),
    [IssuedDate] = DATEADD(month, -24, DATEADD(day, -5, @Now)),
    [CompletedAt] = DATEADD(month, -24, DATEADD(day, -5, @Now))
WHERE [ETRCourseRecordId] = 9;

-- ETR #14: student5@etr.com | Khóa AMT-101 -> Hết hạn 25 ngày trước
UPDATE [ETRCourseRecords]
SET [Status] = 'Completed',
    [IsLocked] = 1,
    [ExpiryDate] = DATEADD(day, -25, @Now),
    [IssuedDate] = DATEADD(month, -24, DATEADD(day, -25, @Now)),
    [CompletedAt] = DATEADD(month, -24, DATEADD(day, -25, @Now))
WHERE [ETRCourseRecordId] = 14;

-- ETR #21: student8@etr.com | Khóa HF-101 -> Hết hạn 8 ngày trước
UPDATE [ETRCourseRecords]
SET [Status] = 'Completed',
    [IsLocked] = 1,
    [ExpiryDate] = DATEADD(day, -8, @Now),
    [IssuedDate] = DATEADD(month, -24, DATEADD(day, -8, @Now)),
    [CompletedAt] = DATEADD(month, -24, DATEADD(day, -8, @Now))
WHERE [ETRCourseRecordId] = 21;

-- ETR #26: student10@etr.com | Khóa B737-TR -> Hết hạn 30 ngày trước
UPDATE [ETRCourseRecords]
SET [Status] = 'Completed',
    [IsLocked] = 1,
    [ExpiryDate] = DATEADD(day, -30, @Now),
    [IssuedDate] = DATEADD(month, -12, DATEADD(day, -30, @Now)),
    [CompletedAt] = DATEADD(month, -12, DATEADD(day, -30, @Now))
WHERE [ETRCourseRecordId] = 26;

-- ETR #76: student26@etr.com | Khóa SMS-101 -> Hết hạn 10 ngày trước
UPDATE [ETRCourseRecords]
SET [Status] = 'Completed',
    [IsLocked] = 1,
    [ExpiryDate] = DATEADD(day, -10, @Now),
    [IssuedDate] = DATEADD(month, -12, DATEADD(day, -10, @Now)),
    [CompletedAt] = DATEADD(month, -12, DATEADD(day, -10, @Now))
WHERE [ETRCourseRecordId] = 76;

-- -------------------------------------------------------------
-- [NHÓM 3: CÒN HIỆU LỰC DÀI - VALID (Làm đối chứng)]
-- -------------------------------------------------------------
-- ETR #37: student13@etr.com | Khóa SEC-101 -> Còn hạn 180 ngày (~6 tháng)
UPDATE [ETRCourseRecords]
SET [Status] = 'Completed',
    [IsLocked] = 1,
    [ExpiryDate] = DATEADD(day, 180, @Now),
    [IssuedDate] = DATEADD(month, -24, DATEADD(day, 180, @Now)),
    [CompletedAt] = DATEADD(month, -24, DATEADD(day, 180, @Now))
WHERE [ETRCourseRecordId] = 37;

-- ETR #46: student16@etr.com | Khóa B737-TR -> Còn hạn 240 ngày (~8 tháng)
UPDATE [ETRCourseRecords]
SET [Status] = 'Completed',
    [IsLocked] = 1,
    [ExpiryDate] = DATEADD(day, 240, @Now),
    [IssuedDate] = DATEADD(month, -12, DATEADD(day, 240, @Now)),
    [CompletedAt] = DATEADD(month, -12, DATEADD(day, 240, @Now))
WHERE [ETRCourseRecordId] = 46;

COMMIT TRANSACTION;

PRINT 'Update completed successfully!';
