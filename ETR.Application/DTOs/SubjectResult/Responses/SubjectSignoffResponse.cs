namespace ETR.Application.DTOs;

public record SubjectSignoffResponse(
    int SubjectSignoffId,
    int SubjectResultId,
    int SignoffByAccountId,
    string Role,
    DateTime SignoffAt,
    string? Comment);
