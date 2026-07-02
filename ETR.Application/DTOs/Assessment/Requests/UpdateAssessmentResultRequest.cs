namespace ETR.Application.DTOs;

public record UpdateAssessmentResultRequest(int AssessmentResultId, int AssessmentComponentId, int LearnerId, int ETRRecordId, decimal Score, string? InstructorComment, int RecordedBy, bool IsPublished);
