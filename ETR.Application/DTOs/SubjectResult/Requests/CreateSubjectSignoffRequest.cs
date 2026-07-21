namespace ETR.Application.DTOs;

public record CreateSubjectSignoffRequest(
    int SubjectResultId,
    string? Comment);
