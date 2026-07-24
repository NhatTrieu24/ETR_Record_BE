using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record CreateAttendanceRecordRequest(
    int SessionId,
    int ClassStudentId,
    [Required, MaxLength(20)] string Status,
    [MaxLength(500)] string? Remarks);
