namespace ETR.Application.DTOs;

public record AssessmentResultResponse(
    int AssessmentResultId,
    int AssessmentId,
    int LearnerId,
    int SubjectResultId,
    decimal Score,
    string ResultStatus,
    int RecordedBy,
    DateTime RecordedAt,
    DateTime? PublishedAt,
    bool IsPublished,
    DateTime? TakenAt,
    string? Remark);
