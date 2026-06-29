namespace ETR.Application.DTOs;

public record CompletionRequirementResponse(int RequirementId, int CourseId, string RequirementName, string? Description, bool IsMandatory, int DisplayOrder);
