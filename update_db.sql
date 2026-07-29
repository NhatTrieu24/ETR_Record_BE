IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [AuditLogId] bigint NOT NULL IDENTITY,
        [AccountId] int NULL,
        [ETRRecordId] int NULL,
        [ActionType] nvarchar(max) NOT NULL,
        [EntityName] nvarchar(max) NOT NULL,
        [RecordId] int NOT NULL,
        [OldValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        [Description] nvarchar(max) NULL,
        [IPAddress] nvarchar(max) NULL,
        [UserAgent] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [CompletionRequirements] (
        [RequirementId] int NOT NULL IDENTITY,
        [CourseId] int NOT NULL,
        [RequirementName] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsMandatory] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [RequirementType] nvarchar(max) NULL,
        [ThresholdValue] decimal(5,2) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_CompletionRequirements] PRIMARY KEY ([RequirementId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [Courses] (
        [CourseId] int NOT NULL IDENTITY,
        [CourseCode] nvarchar(450) NOT NULL,
        [CourseName] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [DurationHours] int NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Courses] PRIMARY KEY ([CourseId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [DashboardSnapshots] (
        [SnapshotId] int NOT NULL IDENTITY,
        [CourseId] int NULL,
        [SnapshotDate] datetime2 NOT NULL,
        [TotalLearners] int NOT NULL,
        [TotalClasses] int NOT NULL,
        [TotalETRs] int NOT NULL,
        [CompletedETRs] int NOT NULL,
        [PendingETRs] int NOT NULL,
        [RejectedETRs] int NOT NULL,
        [MissingEvidenceETRs] int NOT NULL,
        [AverageAttendanceRate] decimal(5,2) NOT NULL,
        [AverageAssessmentScore] decimal(5,2) NOT NULL,
        [GeneratedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_DashboardSnapshots] PRIMARY KEY ([SnapshotId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [Departments] (
        [DepartmentId] int NOT NULL IDENTITY,
        [DepartmentName] nvarchar(450) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([DepartmentId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [EvidenceTypes] (
        [EvidenceTypeId] int NOT NULL IDENTITY,
        [TypeName] nvarchar(450) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_EvidenceTypes] PRIMARY KEY ([EvidenceTypeId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [Roles] (
        [RoleId] int NOT NULL IDENTITY,
        [RoleName] nvarchar(450) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([RoleId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [Subjects] (
        [SubjectId] int NOT NULL IDENTITY,
        [SubjectCode] nvarchar(450) NOT NULL,
        [SubjectName] nvarchar(max) NOT NULL,
        [SubjectType] nvarchar(max) NOT NULL,
        [DefaultHours] int NOT NULL,
        [AssessmentMethod] nvarchar(max) NULL,
        [Description] nvarchar(max) NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Subjects] PRIMARY KEY ([SubjectId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [Classes] (
        [ClassId] int NOT NULL IDENTITY,
        [ClassCode] nvarchar(450) NOT NULL,
        [ClassName] nvarchar(max) NOT NULL,
        [CourseId] int NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [Location] nvarchar(max) NULL,
        [Capacity] int NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Classes] PRIMARY KEY ([ClassId]),
        CONSTRAINT [FK_Classes_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([CourseId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [Accounts] (
        [AccountId] int NOT NULL IDENTITY,
        [Username] nvarchar(450) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [RoleId] int NOT NULL,
        [DepartmentId] int NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Accounts] PRIMARY KEY ([AccountId]),
        CONSTRAINT [FK_Accounts_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([DepartmentId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Accounts_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([RoleId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [CourseSubjects] (
        [CourseId] int NOT NULL,
        [SubjectId] int NOT NULL,
        [SequenceNo] int NOT NULL,
        [RequiredHours] int NOT NULL,
        [PassingScore] decimal(5,2) NOT NULL,
        [IsMandatory] bit NOT NULL,
        [SubjectVersion] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_CourseSubjects] PRIMARY KEY ([CourseId], [SubjectId]),
        CONSTRAINT [FK_CourseSubjects_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([CourseId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CourseSubjects_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([SubjectId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [CourseEnrollments] (
        [EnrollmentId] int NOT NULL IDENTITY,
        [AccountId] int NOT NULL,
        [ClassId] int NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [EnrolledAt] datetime2 NOT NULL,
        [StartDate] datetime2 NULL,
        [ExpectedCompletionDate] datetime2 NULL,
        [ActualCompletionDate] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_CourseEnrollments] PRIMARY KEY ([EnrollmentId]),
        CONSTRAINT [FK_CourseEnrollments_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CourseEnrollments_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([ClassId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [ExportJobs] (
        [ExportJobId] int NOT NULL IDENTITY,
        [RequestedByAccountId] int NOT NULL,
        [ExportType] nvarchar(max) NOT NULL,
        [FileName] nvarchar(max) NULL,
        [FilePath] nvarchar(max) NULL,
        [Status] nvarchar(max) NOT NULL,
        [RequestedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        [DownloadExpiredAt] datetime2 NULL,
        [ETRCourseRecordId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_ExportJobs] PRIMARY KEY ([ExportJobId]),
        CONSTRAINT [FK_ExportJobs_Accounts_RequestedByAccountId] FOREIGN KEY ([RequestedByAccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [Sessions] (
        [SessionId] int NOT NULL IDENTITY,
        [ClassId] int NOT NULL,
        [SubjectId] int NOT NULL,
        [SessionTitle] nvarchar(max) NOT NULL,
        [SessionDate] datetime2 NOT NULL,
        [Location] nvarchar(max) NULL,
        [IsConfirmed] bit NOT NULL,
        [ConfirmedByAccountId] int NULL,
        [ConfirmedAt] datetime2 NULL,
        [IsAssessmentRequired] bit NOT NULL,
        [IsChecklistRequired] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Sessions] PRIMARY KEY ([SessionId]),
        CONSTRAINT [FK_Sessions_Accounts_ConfirmedByAccountId] FOREIGN KEY ([ConfirmedByAccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Sessions_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([ClassId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Sessions_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([SubjectId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [UserProfiles] (
        [AccountId] int NOT NULL,
        [UserCode] nvarchar(max) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [Email] nvarchar(450) NOT NULL,
        [Phone] nvarchar(max) NULL,
        [DateOfBirth] datetime2 NOT NULL,
        [Gender] nvarchar(max) NOT NULL,
        [Organization] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_UserProfiles] PRIMARY KEY ([AccountId]),
        CONSTRAINT [FK_UserProfiles_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [Assessments] (
        [AssessmentId] int NOT NULL IDENTITY,
        [CourseId] int NOT NULL,
        [SubjectId] int NOT NULL,
        [ComponentName] nvarchar(max) NOT NULL,
        [AssessmentType] nvarchar(max) NOT NULL,
        [Weight] decimal(5,2) NOT NULL,
        [PassingScore] decimal(5,2) NOT NULL,
        [IsRequired] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_Assessments] PRIMARY KEY ([AssessmentId]),
        CONSTRAINT [FK_Assessments_CourseSubjects_CourseId_SubjectId] FOREIGN KEY ([CourseId], [SubjectId]) REFERENCES [CourseSubjects] ([CourseId], [SubjectId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [PracticalChecklists] (
        [PracticalChecklistId] int NOT NULL IDENTITY,
        [CourseId] int NOT NULL,
        [SubjectId] int NOT NULL,
        [ItemName] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_PracticalChecklists] PRIMARY KEY ([PracticalChecklistId]),
        CONSTRAINT [FK_PracticalChecklists_CourseSubjects_CourseId_SubjectId] FOREIGN KEY ([CourseId], [SubjectId]) REFERENCES [CourseSubjects] ([CourseId], [SubjectId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [ClassStudents] (
        [ClassStudentId] int NOT NULL IDENTITY,
        [CourseEnrollmentId] int NOT NULL,
        [ClassId] int NOT NULL,
        [AccountId] int NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_ClassStudents] PRIMARY KEY ([ClassStudentId]),
        CONSTRAINT [FK_ClassStudents_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ClassStudents_CourseEnrollments_CourseEnrollmentId] FOREIGN KEY ([CourseEnrollmentId]) REFERENCES [CourseEnrollments] ([EnrollmentId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [ETRCourseRecords] (
        [ETRCourseRecordId] int NOT NULL IDENTITY,
        [EnrollmentId] int NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [SubmittedAt] datetime2 NULL,
        [VerifiedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [IsLocked] bit NOT NULL,
        [CreatedBySystem] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_ETRCourseRecords] PRIMARY KEY ([ETRCourseRecordId]),
        CONSTRAINT [FK_ETRCourseRecords_CourseEnrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [CourseEnrollments] ([EnrollmentId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [AttendanceRecords] (
        [AttendanceRecordId] int NOT NULL IDENTITY,
        [SessionId] int NOT NULL,
        [ClassStudentId] int NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Remarks] nvarchar(max) NULL,
        [RecordedByAccountId] int NOT NULL,
        [RecordedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_AttendanceRecords] PRIMARY KEY ([AttendanceRecordId]),
        CONSTRAINT [FK_AttendanceRecords_ClassStudents_ClassStudentId] FOREIGN KEY ([ClassStudentId]) REFERENCES [ClassStudents] ([ClassStudentId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AttendanceRecords_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [Sessions] ([SessionId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [ApprovalRequests] (
        [ApprovalRequestId] int NOT NULL IDENTITY,
        [ETRCourseRecordId] int NOT NULL,
        [CurrentStatus] nvarchar(max) NOT NULL,
        [SubmittedByAccountId] int NOT NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [CurrentApproverId] int NULL,
        [CompletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_ApprovalRequests] PRIMARY KEY ([ApprovalRequestId]),
        CONSTRAINT [FK_ApprovalRequests_ETRCourseRecords_ETRCourseRecordId] FOREIGN KEY ([ETRCourseRecordId]) REFERENCES [ETRCourseRecords] ([ETRCourseRecordId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [SubjectResults] (
        [SubjectResultId] int NOT NULL IDENTITY,
        [EtrId] int NOT NULL,
        [CourseId] int NOT NULL,
        [SubjectId] int NOT NULL,
        [AttendanceRate] decimal(5,2) NULL,
        [Score] decimal(5,2) NULL,
        [Status] nvarchar(max) NOT NULL,
        [EvaluatedByAccountId] int NULL,
        [EvaluatedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_SubjectResults] PRIMARY KEY ([SubjectResultId]),
        CONSTRAINT [FK_SubjectResults_CourseSubjects_CourseId_SubjectId] FOREIGN KEY ([CourseId], [SubjectId]) REFERENCES [CourseSubjects] ([CourseId], [SubjectId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubjectResults_ETRCourseRecords_EtrId] FOREIGN KEY ([EtrId]) REFERENCES [ETRCourseRecords] ([ETRCourseRecordId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [ApprovalHistories] (
        [ApprovalHistoryId] int NOT NULL IDENTITY,
        [ApprovalRequestId] int NOT NULL,
        [ActionByAccountId] int NOT NULL,
        [ActionType] nvarchar(max) NOT NULL,
        [PreviousStatus] nvarchar(max) NULL,
        [NewStatus] nvarchar(max) NULL,
        [Comments] nvarchar(max) NULL,
        [ActionAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_ApprovalHistories] PRIMARY KEY ([ApprovalHistoryId]),
        CONSTRAINT [FK_ApprovalHistories_Accounts_ActionByAccountId] FOREIGN KEY ([ActionByAccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ApprovalHistories_ApprovalRequests_ApprovalRequestId] FOREIGN KEY ([ApprovalRequestId]) REFERENCES [ApprovalRequests] ([ApprovalRequestId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [AssessmentResults] (
        [AssessmentResultId] int NOT NULL IDENTITY,
        [AssessmentId] int NOT NULL,
        [AccountId] int NOT NULL,
        [SubjectResultId] int NOT NULL,
        [SessionId] int NULL,
        [Score] decimal(5,2) NOT NULL,
        [ResultStatus] nvarchar(max) NOT NULL,
        [GradedByAccountId] int NOT NULL,
        [RecordedAt] datetime2 NOT NULL,
        [PublishedAt] datetime2 NULL,
        [IsPublished] bit NOT NULL,
        [TakenAt] datetime2 NULL,
        [Remark] nvarchar(max) NULL,
        [AttemptNo] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_AssessmentResults] PRIMARY KEY ([AssessmentResultId]),
        CONSTRAINT [FK_AssessmentResults_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AssessmentResults_Accounts_GradedByAccountId] FOREIGN KEY ([GradedByAccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AssessmentResults_Assessments_AssessmentId] FOREIGN KEY ([AssessmentId]) REFERENCES [Assessments] ([AssessmentId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AssessmentResults_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [Sessions] ([SessionId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AssessmentResults_SubjectResults_SubjectResultId] FOREIGN KEY ([SubjectResultId]) REFERENCES [SubjectResults] ([SubjectResultId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [PracticalChecklistResults] (
        [PracticalChecklistResultId] int NOT NULL IDENTITY,
        [SessionId] int NULL,
        [SubjectResultId] int NOT NULL,
        [PracticalChecklistId] int NOT NULL,
        [Score] decimal(5,2) NOT NULL,
        [ResultStatus] nvarchar(max) NOT NULL,
        [VerifiedByAccountId] int NULL,
        [CompletedAt] datetime2 NULL,
        [VerificationComment] nvarchar(max) NULL,
        [IsPublished] bit NOT NULL,
        [PublishedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_PracticalChecklistResults] PRIMARY KEY ([PracticalChecklistResultId]),
        CONSTRAINT [FK_PracticalChecklistResults_Accounts_VerifiedByAccountId] FOREIGN KEY ([VerifiedByAccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PracticalChecklistResults_PracticalChecklists_PracticalChecklistId] FOREIGN KEY ([PracticalChecklistId]) REFERENCES [PracticalChecklists] ([PracticalChecklistId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PracticalChecklistResults_SubjectResults_SubjectResultId] FOREIGN KEY ([SubjectResultId]) REFERENCES [SubjectResults] ([SubjectResultId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [RetakeHistories] (
        [RetakeHistoryId] int NOT NULL IDENTITY,
        [SubjectResultId] int NOT NULL,
        [RetakeDate] datetime2 NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [PreviousScore] decimal(5,2) NOT NULL,
        [NewScore] decimal(5,2) NOT NULL,
        [AuthorizedByAccountId] int NOT NULL,
        [AttemptNo] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_RetakeHistories] PRIMARY KEY ([RetakeHistoryId]),
        CONSTRAINT [FK_RetakeHistories_SubjectResults_SubjectResultId] FOREIGN KEY ([SubjectResultId]) REFERENCES [SubjectResults] ([SubjectResultId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [SubjectSignoffs] (
        [SubjectSignoffId] int NOT NULL IDENTITY,
        [SubjectResultId] int NOT NULL,
        [SignoffByAccountId] int NOT NULL,
        [Role] nvarchar(max) NOT NULL,
        [SignoffAt] datetime2 NOT NULL,
        [Comment] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_SubjectSignoffs] PRIMARY KEY ([SubjectSignoffId]),
        CONSTRAINT [FK_SubjectSignoffs_Accounts_SignoffByAccountId] FOREIGN KEY ([SignoffByAccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SubjectSignoffs_SubjectResults_SubjectResultId] FOREIGN KEY ([SubjectResultId]) REFERENCES [SubjectResults] ([SubjectResultId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE TABLE [EvidenceFiles] (
        [EvidenceFileId] int NOT NULL IDENTITY,
        [EvidenceTypeId] int NOT NULL,
        [UploadedByAccountId] int NOT NULL,
        [AccountId] int NOT NULL,
        [SubjectResultId] int NOT NULL,
        [AttendanceRecordId] int NULL,
        [AssessmentResultId] int NULL,
        [FileName] nvarchar(max) NOT NULL,
        [FilePath] nvarchar(max) NOT NULL,
        [FileExtension] nvarchar(max) NULL,
        [MimeType] nvarchar(max) NULL,
        [FileSize] bigint NOT NULL,
        [VerificationStatus] nvarchar(max) NOT NULL,
        [VerifiedByAccountId] int NULL,
        [VerifiedAt] datetime2 NULL,
        [VerificationComment] nvarchar(max) NULL,
        [UploadedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByAccountId] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedByAccountId] int NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_EvidenceFiles] PRIMARY KEY ([EvidenceFileId]),
        CONSTRAINT [FK_EvidenceFiles_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EvidenceFiles_Accounts_UploadedByAccountId] FOREIGN KEY ([UploadedByAccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EvidenceFiles_Accounts_VerifiedByAccountId] FOREIGN KEY ([VerifiedByAccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EvidenceFiles_AssessmentResults_AssessmentResultId] FOREIGN KEY ([AssessmentResultId]) REFERENCES [AssessmentResults] ([AssessmentResultId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EvidenceFiles_AttendanceRecords_AttendanceRecordId] FOREIGN KEY ([AttendanceRecordId]) REFERENCES [AttendanceRecords] ([AttendanceRecordId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EvidenceFiles_EvidenceTypes_EvidenceTypeId] FOREIGN KEY ([EvidenceTypeId]) REFERENCES [EvidenceTypes] ([EvidenceTypeId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EvidenceFiles_SubjectResults_SubjectResultId] FOREIGN KEY ([SubjectResultId]) REFERENCES [SubjectResults] ([SubjectResultId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_Accounts_DepartmentId] ON [Accounts] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_Accounts_RoleId] ON [Accounts] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Accounts_Username] ON [Accounts] ([Username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_ApprovalHistories_ActionByAccountId] ON [ApprovalHistories] ([ActionByAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_ApprovalHistories_ApprovalRequestId] ON [ApprovalHistories] ([ApprovalRequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_ApprovalRequests_ETRCourseRecordId] ON [ApprovalRequests] ([ETRCourseRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_AssessmentResults_AccountId] ON [AssessmentResults] ([AccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AssessmentResults_AssessmentId_AccountId_SessionId] ON [AssessmentResults] ([AssessmentId], [AccountId], [SessionId]) WHERE [SessionId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_AssessmentResults_GradedByAccountId] ON [AssessmentResults] ([GradedByAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_AssessmentResults_SessionId] ON [AssessmentResults] ([SessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_AssessmentResults_SubjectResultId] ON [AssessmentResults] ([SubjectResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_Assessments_CourseId_SubjectId] ON [Assessments] ([CourseId], [SubjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_AttendanceRecords_ClassStudentId] ON [AttendanceRecords] ([ClassStudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AttendanceRecords_SessionId_ClassStudentId] ON [AttendanceRecords] ([SessionId], [ClassStudentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Classes_ClassCode] ON [Classes] ([ClassCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_Classes_CourseId] ON [Classes] ([CourseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_ClassStudents_AccountId] ON [ClassStudents] ([AccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_ClassStudents_CourseEnrollmentId] ON [ClassStudents] ([CourseEnrollmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CourseEnrollments_AccountId_ClassId] ON [CourseEnrollments] ([AccountId], [ClassId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_CourseEnrollments_ClassId] ON [CourseEnrollments] ([ClassId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Courses_CourseCode] ON [Courses] ([CourseCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_CourseSubjects_SubjectId] ON [CourseSubjects] ([SubjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Departments_DepartmentName] ON [Departments] ([DepartmentName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ETRCourseRecords_EnrollmentId] ON [ETRCourseRecords] ([EnrollmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_EvidenceFiles_AccountId] ON [EvidenceFiles] ([AccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_EvidenceFiles_AssessmentResultId] ON [EvidenceFiles] ([AssessmentResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_EvidenceFiles_AttendanceRecordId] ON [EvidenceFiles] ([AttendanceRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_EvidenceFiles_EvidenceTypeId] ON [EvidenceFiles] ([EvidenceTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_EvidenceFiles_SubjectResultId] ON [EvidenceFiles] ([SubjectResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_EvidenceFiles_UploadedByAccountId] ON [EvidenceFiles] ([UploadedByAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_EvidenceFiles_VerifiedByAccountId] ON [EvidenceFiles] ([VerifiedByAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EvidenceTypes_TypeName] ON [EvidenceTypes] ([TypeName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_ExportJobs_RequestedByAccountId] ON [ExportJobs] ([RequestedByAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_PracticalChecklistResults_PracticalChecklistId] ON [PracticalChecklistResults] ([PracticalChecklistId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PracticalChecklistResults_SubjectResultId_PracticalChecklistId] ON [PracticalChecklistResults] ([SubjectResultId], [PracticalChecklistId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_PracticalChecklistResults_VerifiedByAccountId] ON [PracticalChecklistResults] ([VerifiedByAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_PracticalChecklists_CourseId_SubjectId] ON [PracticalChecklists] ([CourseId], [SubjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_RetakeHistories_SubjectResultId] ON [RetakeHistories] ([SubjectResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_RoleName] ON [Roles] ([RoleName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_Sessions_ClassId] ON [Sessions] ([ClassId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_Sessions_ConfirmedByAccountId] ON [Sessions] ([ConfirmedByAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_Sessions_SubjectId] ON [Sessions] ([SubjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_SubjectResults_CourseId_SubjectId] ON [SubjectResults] ([CourseId], [SubjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SubjectResults_EtrId_CourseId_SubjectId] ON [SubjectResults] ([EtrId], [CourseId], [SubjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Subjects_SubjectCode] ON [Subjects] ([SubjectCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_SubjectSignoffs_SignoffByAccountId] ON [SubjectSignoffs] ([SignoffByAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    CREATE INDEX [IX_SubjectSignoffs_SubjectResultId] ON [SubjectSignoffs] ([SubjectResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_UserProfiles_Email] ON [UserProfiles] ([Email]) WHERE [Email] IS NOT NULL AND [Email] <> ''''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082245_CleanBaseData'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724082245_CleanBaseData', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725182752_AddCertificateValidityAndRecurrent'
)
BEGIN
    ALTER TABLE [ETRCourseRecords] ADD [ExpiryDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725182752_AddCertificateValidityAndRecurrent'
)
BEGIN
    ALTER TABLE [ETRCourseRecords] ADD [IssuedDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725182752_AddCertificateValidityAndRecurrent'
)
BEGIN
    ALTER TABLE [ETRCourseRecords] ADD [PreviousRecordId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725182752_AddCertificateValidityAndRecurrent'
)
BEGIN
    ALTER TABLE [Courses] ADD [CourseType] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725182752_AddCertificateValidityAndRecurrent'
)
BEGIN
    ALTER TABLE [Courses] ADD [ValidityMonths] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725182752_AddCertificateValidityAndRecurrent'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725182752_AddCertificateValidityAndRecurrent', N'9.0.0');
END;

COMMIT;
GO

