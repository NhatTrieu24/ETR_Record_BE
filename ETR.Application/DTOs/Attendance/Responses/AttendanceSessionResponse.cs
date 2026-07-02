namespace ETR.Application.DTOs;

public record AttendanceSessionResponse(
    int AttendanceSessionId,
    int ClassId,
    string SessionTitle,
    DateTime SessionDate,
    string? Location,
    bool IsConfirmed,
    int? ConfirmedBy,
    DateTime? ConfirmedAt);
