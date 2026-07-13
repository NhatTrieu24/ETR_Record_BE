namespace ETR.Application.DTOs;

public record CreateSubjectSignoffRequest(
    int SubjectResultId,
    string Role,
    string? Comment);
