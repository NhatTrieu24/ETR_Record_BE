namespace ETR.Application.DTOs;

public record CreateAttendanceSessionRequest(int ClassId, string SessionTitle, DateTime SessionDate, string? Location);
