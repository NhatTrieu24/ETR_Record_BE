using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record UpdateCourseRequest(
    int CourseId,
    [Required, MaxLength(20)] string CourseCode,
    [Required, MaxLength(200)] string CourseName,
    [MaxLength(2000)] string Description,
    int DurationHours,
    [Required, MaxLength(20)] string Status);
