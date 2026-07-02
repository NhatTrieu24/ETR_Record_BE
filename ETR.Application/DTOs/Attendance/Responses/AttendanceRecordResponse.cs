namespace ETR.Application.DTOs;

public record AttendanceRecordResponse(
    int AttendanceRecordId,
    int AttendanceSessionId,
    int LearnerId,
    int ETRRecordId,
    string Status,
    string? Remarks,
    int RecordedBy,
    DateTime RecordedAt);
