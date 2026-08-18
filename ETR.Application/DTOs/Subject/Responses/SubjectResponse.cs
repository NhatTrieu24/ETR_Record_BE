using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record SubjectResponse(int SubjectId, string SubjectCode, string SubjectName, string SubjectType, int DefaultHours, string? AssessmentMethod, string? Description, SubjectStatus Status);
