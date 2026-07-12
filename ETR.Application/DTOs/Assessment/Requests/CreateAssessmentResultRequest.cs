namespace ETR.Application.DTOs;

public record CreateAssessmentResultRequest(
    int AssessmentId,
    int LearnerId,
    int SubjectResultId,
    decimal Score,
    string? Remark,
    int RecordedBy);
