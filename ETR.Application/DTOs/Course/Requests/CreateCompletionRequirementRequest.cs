namespace ETR.Application.DTOs;

public record CreateCompletionRequirementRequest(int CourseId, string RequirementName, string? Description, bool IsMandatory, int DisplayOrder);
