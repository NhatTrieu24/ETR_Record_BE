namespace ETR.Application.DTOs;

public record CreateAssessmentResultRequest(int AssessmentComponentId, int LearnerId, int ETRRecordId, decimal Score, string? InstructorComment, int RecordedBy);
