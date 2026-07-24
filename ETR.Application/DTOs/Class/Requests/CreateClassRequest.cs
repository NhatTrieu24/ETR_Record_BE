using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record CreateClassRequest(
    [Required, MaxLength(20)] string ClassCode,
    [Required, MaxLength(200)] string ClassName,
    int CourseId,
    DateTime StartDate,
    DateTime EndDate,
    [MaxLength(200)] string? Location,
    int Capacity,
    [Required, MaxLength(20)] string Status);
