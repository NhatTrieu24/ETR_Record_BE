namespace ETR.Application.DTOs;

public record UpdateAttendanceRecordRequest(int AttendanceRecordId, int AttendanceSessionId, int LearnerId, int ETRRecordId, string Status, string? Remarks, int RecordedBy);
