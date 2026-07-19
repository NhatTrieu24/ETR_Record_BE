namespace ETR.Application.DTOs;

public record UpdateSubjectRequest(int SubjectId, string SubjectCode, string SubjectName, string SubjectType, int DefaultHours, string? AssessmentMethod, string? Description, string Status);
