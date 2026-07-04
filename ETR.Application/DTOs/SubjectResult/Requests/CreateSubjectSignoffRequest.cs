namespace ETR.Application.DTOs;

public record CreateSubjectSignoffRequest(
    int SubjectResultId,
    int SignoffBy,
    string Role,
    string? Comment);
