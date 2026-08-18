using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record AttendanceRecordResponse(
    int AttendanceRecordId,
    int SessionId,
    int EnrollmentId,
    AttendanceStatus Status,
    string? Remarks,
    int RecordedByAccountId,
    DateTime RecordedAt);
