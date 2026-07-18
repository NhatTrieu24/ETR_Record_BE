namespace ETR.Application.DTOs;

public record AssessmentResultResponse(
    int AssessmentResultId,
    int AssessmentId,
    int AccountId,
    int SubjectResultId,
    decimal Score,
    string ResultStatus,
    int GradedByAccountId,
    DateTime RecordedAt,
    DateTime? PublishedAt,
    bool IsPublished,
    DateTime? TakenAt,
    string? Remark);
