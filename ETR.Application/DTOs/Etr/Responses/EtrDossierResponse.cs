using System;
using System.Collections.Generic;
using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record EtrDossierResponse(
    int ETRCourseRecordId,
    int EnrollmentId,
    EtrStatus Status,
    bool IsLocked,
    int CourseVersionNo,
    DateTime? SubmittedAt,
    DateTime? VerifiedAt,
    DateTime? CompletedAt,
    DateTime? IssuedDate,
    DateTime? ExpiryDate,
    EtrDossierStudentInfo Student,
    EtrDossierCourseInfo Course,
    EtrDossierClassInfo Class,
    EtrDossierReadinessSummary Readiness,
    IEnumerable<EtrDossierSubjectItem> Subjects,
    EtrDossierCredentialSummary? Credentials,
    IEnumerable<EtrDossierApprovalHistoryItem> ApprovalHistories,
    IEnumerable<string> AllowedActions
);

public record EtrDossierStudentInfo(
    int AccountId,
    string UserCode,
    string FullName,
    string? Email,
    string? Phone
);

public record EtrDossierCourseInfo(
    int CourseId,
    string CourseCode,
    string CourseName,
    int VersionNo
);

public record EtrDossierClassInfo(
    int ClassId,
    string ClassCode,
    string ClassName,
    DateTime? StartDate,
    DateTime? EndDate,
    string Status
);

public record EtrDossierReadinessSummary(
    int TotalSubjects,
    int PassedSubjects,
    decimal AverageAttendance,
    decimal TotalFlightHours,
    decimal TotalSimulatorHours,
    string OverallReadinessStatus,
    IEnumerable<string> PendingConditions
);

public record EtrDossierSubjectItem(
    int SubjectResultId,
    int SubjectId,
    string SubjectCode,
    string SubjectName,
    string SubjectType,
    int RequiredHours,
    decimal PassingScore,
    bool IsMandatory,
    SubjectResultStatus Status,
    decimal? Score,
    decimal? AttendanceRate,
    bool IsSignedOff,
    DateTime? SignedOffAt,
    string? SignoffByName,
    string? SignoffRole,
    string? SignoffComment,
    bool IsCarriedOver,
    IEnumerable<EtrDossierAssessmentItem> Assessments,
    IEnumerable<EtrDossierChecklistItem> PracticalChecklists,
    IEnumerable<EtrDossierSessionItem> Sessions,
    IEnumerable<EtrDossierEvidenceItem> EvidenceFiles
);

public record EtrDossierAssessmentItem(
    int AssessmentResultId,
    int AssessmentId,
    string ComponentName,
    string AssessmentType,
    decimal Weight,
    decimal PassingScore,
    decimal Score,
    string ResultStatus,
    int AttemptNo,
    DateTime? TakenAt,
    string? Remark,
    string? GradedByName
);

public record EtrDossierChecklistItem(
    int PracticalChecklistResultId,
    int PracticalChecklistId,
    string ItemName,
    string ResultStatus,
    string? VerifiedByName,
    DateTime? CompletedAt,
    string? VerificationComment
);

public record EtrDossierSessionItem(
    int SessionId,
    string SessionTitle,
    DateTime? SessionDate,
    string TrainingType,
    string? LessonCode,
    string? Location,
    string AttendanceStatus,
    decimal? FlightHours,
    decimal? SimulatorHours,
    decimal? DualHours,
    decimal? SoloHours,
    decimal? PicHours,
    decimal? CrossCountryHours,
    decimal? NightHours,
    decimal? InstrumentHours,
    string? DepartureIcao,
    string? ArrivalIcao,
    string? Route,
    string? AircraftRegistration,
    string? SimulatorDevice,
    int? AssignedInstructorAccountId,
    string? AssignedInstructorName,
    int? SignedInstructorAccountId,
    string? SignedInstructorName,
    DateTime? InstructorSignedAt,
    string ActualInstructorNote,
    string InstructorQualificationNotice,
    string? InstructorComments,
    string? StudentComments
);

public record EtrDossierEvidenceItem(
    int EvidenceFileId,
    string FileName,
    string FileUrl,
    string FileType,
    string VerificationStatus,
    string? VerificationComment,
    string? VerifiedByName,
    DateTime? VerifiedAt,
    string? UploadedByName,
    DateTime UploadedAt
);

public record EtrDossierCredentialSummary(
    bool IsCredentialsVerified,
    string? LicenseType,
    string? LicenseNumber,
    DateTime? LicenseExpiryDate,
    string? MedicalClass,
    DateTime? MedicalExpiryDate,
    int? IcaoElpLevel,
    DateTime? IcaoElpExpiryDate,
    string? TypeRatings,
    IEnumerable<CredentialAttachmentDto> Attachments
);

public record EtrDossierApprovalHistoryItem(
    int ApprovalHistoryId,
    int? ApprovalRequestId,
    string ActionType,
    string? PreviousStatus,
    string? NewStatus,
    string? Comments,
    int ActionByAccountId,
    string ActionByName,
    DateTime ActionAt
);
