namespace ETR.Application.DTOs.Assessment.Responses;

public record AssessmentResponse(
    int AssessmentId,
    int CourseId,
    int SubjectId,
    string ComponentName,
    string AssessmentType,
    decimal Weight,
    decimal PassingScore,
    bool IsRequired,
    int DisplayOrder
);
