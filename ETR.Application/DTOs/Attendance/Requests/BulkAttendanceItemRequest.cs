namespace ETR.Application.DTOs;

public record BulkAttendanceItemRequest(
    int AttendanceSessionId,
    int LearnerId,
    int EtrRecordId,
    string Status,
    string? Remarks);
