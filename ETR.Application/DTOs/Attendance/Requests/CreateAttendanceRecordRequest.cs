namespace ETR.Application.DTOs;

public record CreateAttendanceRecordRequest(
    int SessionId,
    int LearnerId,
    int EnrollmentId,
    string Status,
    string? Remarks);
