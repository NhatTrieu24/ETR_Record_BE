using System.ComponentModel.DataAnnotations;
using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record UpdateAttendanceRecordRequest(
    [Required] AttendanceStatus Status,
    [MaxLength(500)] string? Remarks
);
