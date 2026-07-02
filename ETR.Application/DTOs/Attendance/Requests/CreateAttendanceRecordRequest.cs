namespace ETR.Application.DTOs;

public record CreateAttendanceRecordRequest(int AttendanceSessionId, int LearnerId, int ETRRecordId, string Status, string? Remarks, int RecordedBy);
