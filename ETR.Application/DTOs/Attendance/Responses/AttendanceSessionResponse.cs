namespace ETR.Application.DTOs;

public record AttendanceSessionResponse(
    int SessionId,
    int ClassId,
    int SubjectId,
    string SessionTitle,
    DateTime SessionDate,
    string? Location,
    bool IsConfirmed,
    int? ConfirmedBy,
    DateTime? ConfirmedAt);
