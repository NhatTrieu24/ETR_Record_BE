namespace ETR.Application.DTOs;

public record CreateAssessmentResultRequest(
    int AssessmentId,
    int AccountId,
    int SubjectResultId,
    decimal Score,
    string? Remark,
    int? SessionId = null);
