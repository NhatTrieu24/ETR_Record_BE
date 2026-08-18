using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record EtrDetailsResponse(
    int ETRCourseRecordId,
    int EnrollmentId,
    EtrStatus Status,
    bool IsLocked,
    DateTime? SubmittedAt,
    DateTime? VerifiedAt,
    DateTime? CompletedAt,
    IEnumerable<EtrSubjectDetailResponse> SubjectResults,
    IEnumerable<EtrApprovalHistoryResponse> ApprovalHistories,
    IEnumerable<EtrEvidenceFileResponse> EvidenceFiles
);

public record EtrSubjectDetailResponse(
    int SubjectResultId,
    int SubjectId,
    SubjectResultStatus Status,
    DateTime CreatedAt,
    decimal? AttendanceRate,
    decimal? Score,
    bool IsSignedOff,
    DateTime? SignedOffAt,
    IEnumerable<EtrAssessmentResultResponse> AssessmentResults,
    IEnumerable<EtrPracticalChecklistResultResponse> PracticalChecklistResults
);

public record EtrAssessmentResultResponse(
    int AssessmentResultId,
    int AssessmentId,
    decimal Score,
    string ResultStatus,
    int AttemptNo,
    bool IsPublished
);

public record EtrPracticalChecklistResultResponse(
    int PracticalChecklistResultId,
    int PracticalChecklistId,
    string ResultStatus,
    bool IsPublished
);

public record EtrApprovalHistoryResponse(
    int ApprovalHistoryId,
    int? ApprovalRequestId,
    string ActionType,
    string? Comments,
    int ActionByAccountId,
    DateTime ActionAt
);

public record EtrEvidenceFileResponse(
    int EvidenceFileId,
    string FileName,
    string FilePath,
    string FileType,
    int UploadedByAccountId,
    DateTime UploadedAt
);
