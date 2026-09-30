using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record SignAttendanceRecordRequest(
    [MaxLength(1000)] string? Comments = null
);
