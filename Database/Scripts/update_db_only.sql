BEGIN TRANSACTION;
ALTER TABLE [ETRCourseRecords] ADD [ExpiryDate] datetime2 NULL;

ALTER TABLE [ETRCourseRecords] ADD [IssuedDate] datetime2 NULL;

ALTER TABLE [ETRCourseRecords] ADD [PreviousRecordId] int NULL;

ALTER TABLE [Courses] ADD [CourseType] nvarchar(max) NULL;

ALTER TABLE [Courses] ADD [ValidityMonths] int NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260725182752_AddCertificateValidityAndRecurrent', N'9.0.0');

COMMIT;
GO

