using System.ComponentModel.DataAnnotations;
using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record CreateAttendanceRecordRequest(
    int SessionId,
    int EnrollmentId,
    [Required] AttendanceStatus Status,
    [MaxLength(500)] string? Remarks);
