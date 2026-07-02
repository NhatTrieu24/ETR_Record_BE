namespace ETR.Application.DTOs;

public record UpdateLearnerTypeRequest(int LearnerTypeId, string TypeName, string? Description);
