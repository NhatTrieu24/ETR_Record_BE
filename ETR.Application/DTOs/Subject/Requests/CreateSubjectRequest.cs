namespace ETR.Application.DTOs;

public record CreateSubjectRequest(string SubjectCode, string SubjectName, string SubjectType, int DefaultHours, string? AssessmentMethod, string? Description, string Status);
