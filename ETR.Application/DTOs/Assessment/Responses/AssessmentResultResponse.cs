namespace ETR.Application.DTOs;

public record AssessmentResultResponse(
    int AssessmentResultId,
    int AssessmentComponentId,
    int LearnerId,
    int ETRRecordId,
    decimal Score,
    string ResultStatus,
    string? InstructorComment,
    int RecordedBy,
    DateTime RecordedAt,
    DateTime? PublishedAt,
    bool IsPublished);
