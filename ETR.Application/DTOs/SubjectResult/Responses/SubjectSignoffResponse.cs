namespace ETR.Application.DTOs;

public record SubjectSignoffResponse(
    int SubjectSignoffId,
    int SubjectResultId,
    int SignoffBy,
    string Role,
    DateTime SignoffAt,
    string? Comment);
