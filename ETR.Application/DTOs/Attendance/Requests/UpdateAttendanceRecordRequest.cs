using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record UpdateAttendanceRecordRequest(
    [Required, RegularExpression("^(Present|Absent|Late)$", ErrorMessage = "Status must be one of: Present, Absent, Late.")] string Status,
    [MaxLength(500)] string? Remarks
);
