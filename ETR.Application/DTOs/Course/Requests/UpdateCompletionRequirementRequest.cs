namespace ETR.Application.DTOs;

public record UpdateCompletionRequirementRequest(int RequirementId, int CourseId, string RequirementName, string? Description, bool IsMandatory, int DisplayOrder);
