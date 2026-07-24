using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record UpdateAttendanceRecordRequest(
    [Required, MaxLength(20)] string Status,
    [MaxLength(500)] string? Remarks
);
