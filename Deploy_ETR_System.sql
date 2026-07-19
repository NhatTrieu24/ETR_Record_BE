SET QUOTED_IDENTIFIER ON;
GO
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ETRManagementDB')
BEGIN
    CREATE DATABASE [ETRManagementDB];
END
GO
USE [ETRManagementDB];
GO
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

CREATE TABLE [CompletionRequirements] (
    [RequirementId] int NOT NULL IDENTITY,
    [CourseId] int NOT NULL,
    [RequirementName] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsMandatory] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedByAccountId] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedByAccountId] int NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_CompletionRequirements] PRIMARY KEY ([RequirementId])
);

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
    [CreatedAt] datetime2 NOT NULL,
    [CreatedByAccountId] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedByAccountId] int NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    CONSTRAINT [PK_ExportJobs] PRIMARY KEY ([ExportJobId]),
    CONSTRAINT [FK_ExportJobs_Accounts_RequestedByAccountId] FOREIGN KEY ([RequestedByAccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION
);

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

CREATE TABLE [AssessmentResults] (
    [AssessmentResultId] int NOT NULL IDENTITY,
    [AssessmentId] int NOT NULL,
    [AccountId] int NOT NULL,
    [SubjectResultId] int NOT NULL,
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
    CONSTRAINT [FK_AssessmentResults_SubjectResults_SubjectResultId] FOREIGN KEY ([SubjectResultId]) REFERENCES [SubjectResults] ([SubjectResultId]) ON DELETE NO ACTION
);

CREATE TABLE [PracticalChecklistResults] (
    [PracticalChecklistResultId] int NOT NULL IDENTITY,
    [SubjectResultId] int NOT NULL,
    [PracticalChecklistId] int NOT NULL,
    [IsCompleted] bit NOT NULL,
    [VerifiedByAccountId] int NULL,
    [CompletedAt] datetime2 NULL,
    [VerificationComment] nvarchar(max) NULL,
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

CREATE INDEX [IX_Accounts_DepartmentId] ON [Accounts] ([DepartmentId]);

CREATE INDEX [IX_Accounts_RoleId] ON [Accounts] ([RoleId]);

CREATE UNIQUE INDEX [IX_Accounts_Username] ON [Accounts] ([Username]);

CREATE INDEX [IX_ApprovalHistories_ActionByAccountId] ON [ApprovalHistories] ([ActionByAccountId]);

CREATE INDEX [IX_ApprovalHistories_ApprovalRequestId] ON [ApprovalHistories] ([ApprovalRequestId]);

CREATE INDEX [IX_ApprovalRequests_ETRCourseRecordId] ON [ApprovalRequests] ([ETRCourseRecordId]);

CREATE INDEX [IX_AssessmentResults_AccountId] ON [AssessmentResults] ([AccountId]);

CREATE UNIQUE INDEX [IX_AssessmentResults_AssessmentId_AccountId] ON [AssessmentResults] ([AssessmentId], [AccountId]);

CREATE INDEX [IX_AssessmentResults_GradedByAccountId] ON [AssessmentResults] ([GradedByAccountId]);

CREATE INDEX [IX_AssessmentResults_SubjectResultId] ON [AssessmentResults] ([SubjectResultId]);

CREATE INDEX [IX_Assessments_CourseId_SubjectId] ON [Assessments] ([CourseId], [SubjectId]);

CREATE INDEX [IX_AttendanceRecords_ClassStudentId] ON [AttendanceRecords] ([ClassStudentId]);

CREATE UNIQUE INDEX [IX_AttendanceRecords_SessionId_ClassStudentId] ON [AttendanceRecords] ([SessionId], [ClassStudentId]);

CREATE UNIQUE INDEX [IX_Classes_ClassCode] ON [Classes] ([ClassCode]);

CREATE INDEX [IX_Classes_CourseId] ON [Classes] ([CourseId]);

CREATE INDEX [IX_ClassStudents_AccountId] ON [ClassStudents] ([AccountId]);

CREATE INDEX [IX_ClassStudents_CourseEnrollmentId] ON [ClassStudents] ([CourseEnrollmentId]);

CREATE UNIQUE INDEX [IX_CourseEnrollments_AccountId_ClassId] ON [CourseEnrollments] ([AccountId], [ClassId]);

CREATE INDEX [IX_CourseEnrollments_ClassId] ON [CourseEnrollments] ([ClassId]);

CREATE UNIQUE INDEX [IX_Courses_CourseCode] ON [Courses] ([CourseCode]);

CREATE INDEX [IX_CourseSubjects_SubjectId] ON [CourseSubjects] ([SubjectId]);

CREATE UNIQUE INDEX [IX_Departments_DepartmentName] ON [Departments] ([DepartmentName]);

CREATE UNIQUE INDEX [IX_ETRCourseRecords_EnrollmentId] ON [ETRCourseRecords] ([EnrollmentId]);

CREATE INDEX [IX_EvidenceFiles_AccountId] ON [EvidenceFiles] ([AccountId]);

CREATE INDEX [IX_EvidenceFiles_AssessmentResultId] ON [EvidenceFiles] ([AssessmentResultId]);

CREATE INDEX [IX_EvidenceFiles_AttendanceRecordId] ON [EvidenceFiles] ([AttendanceRecordId]);

CREATE INDEX [IX_EvidenceFiles_EvidenceTypeId] ON [EvidenceFiles] ([EvidenceTypeId]);

CREATE INDEX [IX_EvidenceFiles_SubjectResultId] ON [EvidenceFiles] ([SubjectResultId]);

CREATE INDEX [IX_EvidenceFiles_UploadedByAccountId] ON [EvidenceFiles] ([UploadedByAccountId]);

CREATE INDEX [IX_EvidenceFiles_VerifiedByAccountId] ON [EvidenceFiles] ([VerifiedByAccountId]);

CREATE UNIQUE INDEX [IX_EvidenceTypes_TypeName] ON [EvidenceTypes] ([TypeName]);

CREATE INDEX [IX_ExportJobs_RequestedByAccountId] ON [ExportJobs] ([RequestedByAccountId]);

CREATE INDEX [IX_PracticalChecklistResults_PracticalChecklistId] ON [PracticalChecklistResults] ([PracticalChecklistId]);

CREATE UNIQUE INDEX [IX_PracticalChecklistResults_SubjectResultId_PracticalChecklistId] ON [PracticalChecklistResults] ([SubjectResultId], [PracticalChecklistId]);

CREATE INDEX [IX_PracticalChecklistResults_VerifiedByAccountId] ON [PracticalChecklistResults] ([VerifiedByAccountId]);

CREATE INDEX [IX_PracticalChecklists_CourseId_SubjectId] ON [PracticalChecklists] ([CourseId], [SubjectId]);

CREATE INDEX [IX_RetakeHistories_SubjectResultId] ON [RetakeHistories] ([SubjectResultId]);

CREATE UNIQUE INDEX [IX_Roles_RoleName] ON [Roles] ([RoleName]);

CREATE INDEX [IX_Sessions_ClassId] ON [Sessions] ([ClassId]);

CREATE INDEX [IX_Sessions_ConfirmedByAccountId] ON [Sessions] ([ConfirmedByAccountId]);

CREATE INDEX [IX_Sessions_SubjectId] ON [Sessions] ([SubjectId]);

CREATE INDEX [IX_SubjectResults_CourseId_SubjectId] ON [SubjectResults] ([CourseId], [SubjectId]);

CREATE UNIQUE INDEX [IX_SubjectResults_EtrId_CourseId_SubjectId] ON [SubjectResults] ([EtrId], [CourseId], [SubjectId]);

CREATE UNIQUE INDEX [IX_Subjects_SubjectCode] ON [Subjects] ([SubjectCode]);

CREATE INDEX [IX_SubjectSignoffs_SignoffByAccountId] ON [SubjectSignoffs] ([SignoffByAccountId]);

CREATE INDEX [IX_SubjectSignoffs_SubjectResultId] ON [SubjectSignoffs] ([SubjectResultId]);

CREATE UNIQUE INDEX [IX_UserProfiles_Email] ON [UserProfiles] ([Email]) WHERE [Email] IS NOT NULL AND [Email] <> '';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260719063606_InitialCreate', N'9.0.0');


                DECLARE @Now DATETIME2 = GETUTCDATE();

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

                -- Seed Roles
                SET IDENTITY_INSERT [Roles] ON;
                INSERT INTO [Roles] (RoleId, RoleName, Description, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'Admin', 'System Administrator', @Now, 0, NULL),
                (2, 'Instructor', 'Course Instructor', @Now, 0, NULL),
                (3, 'QA', 'Quality Assurance', @Now, 0, NULL),
                (4, 'Academic', 'Academic Staff', @Now, 0, NULL),
                (5, 'TrainingManager', 'Training Manager', @Now, 0, NULL),
                (6, 'Student', 'Student / Learner', @Now, 0, NULL);
                SET IDENTITY_INSERT [Roles] OFF;

                -- Seed Departments (Dummy data just so FKs work)
                SET IDENTITY_INSERT [Departments] ON;
                INSERT INTO [Departments] (DepartmentId, DepartmentName, Description, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'Administration', 'General Admin', @Now, 0, NULL),
                (2, 'Training', 'Training Dept', @Now, 0, NULL);
                SET IDENTITY_INSERT [Departments] OFF;

                -- Seed Accounts
                SET IDENTITY_INSERT [Accounts] ON;
                INSERT INTO [Accounts] (AccountId, Username, PasswordHash, RoleId, DepartmentId, Status, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'admin@etr.com', '123456', 1, 1, 'Active', @Now, 0, NULL),
                (2, 'instructor@etr.com', '123456', 2, 2, 'Active', @Now, 0, NULL),
                (3, 'qa@etr.com', '123456', 3, 1, 'Active', @Now, 0, NULL),
                (4, 'academic@etr.com', '123456', 4, 1, 'Active', @Now, 0, NULL),
                (5, 'manager@etr.com', '123456', 5, 2, 'Active', @Now, 0, NULL),
                (6, 'student@etr.com', '123456', 6, 2, 'Active', @Now, 0, NULL);
                SET IDENTITY_INSERT [Accounts] OFF;

                -- Seed UserProfiles
                INSERT INTO [UserProfiles] (AccountId, UserCode, FullName, Email, DateOfBirth, Gender, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'ADM-01', 'System Admin', 'admin@etr.com', '1980-01-01', 'Other', @Now, 0, 1),
                (2, 'INS-01', 'Senior Instructor', 'instructor@etr.com', '1985-01-01', 'Male', @Now, 0, 1),
                (3, 'QA-01', 'QA Specialist', 'qa@etr.com', '1990-01-01', 'Female', @Now, 0, 1),
                (4, 'ACA-01', 'Academic Staff', 'academic@etr.com', '1992-01-01', 'Female', @Now, 0, 1),
                (5, 'MGR-01', 'Training Manager', 'manager@etr.com', '1988-01-01', 'Male', @Now, 0, 1),
                (6, 'STU-01', 'Jane Student', 'student@etr.com', '2000-01-01', 'Female', @Now, 0, 1);
            

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260719063637_SeedSystemData', N'9.0.0');

COMMIT;
GO


