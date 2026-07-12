namespace ETR.Application.DTOs;

public record AttendanceRecordResponse(
    int AttendanceRecordId,
    int SessionId,
    int LearnerId,
    int EnrollmentId,
    string Status,
    string? Remarks,
    int RecordedBy,
    DateTime RecordedAt);
