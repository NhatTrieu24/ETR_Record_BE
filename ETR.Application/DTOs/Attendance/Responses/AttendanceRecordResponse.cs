namespace ETR.Application.DTOs;

public record AttendanceRecordResponse(
    int AttendanceRecordId,
    int SessionId,
    int EnrollmentId,
    string Status,
    string? Remarks,
    int RecordedByAccountId,
    DateTime RecordedAt);
