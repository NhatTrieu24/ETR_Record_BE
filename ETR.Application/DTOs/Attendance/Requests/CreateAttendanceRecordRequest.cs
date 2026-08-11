using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record CreateAttendanceRecordRequest(
    int SessionId,
    int EnrollmentId,
    [Required, RegularExpression("^(Present|Absent)$", ErrorMessage = "Status must be one of: Present, Absent.")] string Status,
    [MaxLength(500)] string? Remarks);
