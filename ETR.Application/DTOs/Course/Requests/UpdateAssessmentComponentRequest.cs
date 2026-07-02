namespace ETR.Application.DTOs;

public record UpdateAssessmentComponentRequest(int AssessmentComponentId, int CourseId, string ComponentName, string AssessmentType, decimal Weight, decimal PassingScore, bool IsRequired, int DisplayOrder);
