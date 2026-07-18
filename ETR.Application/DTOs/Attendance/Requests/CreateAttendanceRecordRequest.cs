namespace ETR.Application.DTOs;

public record CreateAttendanceRecordRequest(
    int SessionId,
    int ClassStudentId,
    string Status,
    string? Remarks);
