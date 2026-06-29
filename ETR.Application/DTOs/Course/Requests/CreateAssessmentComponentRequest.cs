namespace ETR.Application.DTOs;

public record CreateAssessmentComponentRequest(int CourseId, string ComponentName, string AssessmentType, decimal Weight, decimal PassingScore, bool IsRequired, int DisplayOrder);
