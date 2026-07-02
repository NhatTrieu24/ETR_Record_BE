namespace ETR.Application.DTOs;

public record UpdateAttendanceSessionRequest(int AttendanceSessionId, int ClassId, string SessionTitle, DateTime SessionDate, string? Location, bool IsConfirmed);
