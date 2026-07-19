namespace ETR.Application.DTOs;

public record UpdateAttendanceRecordRequest(
    string Status,
    string? Remarks
);
