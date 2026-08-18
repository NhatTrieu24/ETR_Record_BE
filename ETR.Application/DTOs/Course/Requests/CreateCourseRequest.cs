using System.ComponentModel.DataAnnotations;
using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record CreateCourseRequest(
    [Required, MaxLength(20)] string CourseCode,
    [Required, MaxLength(200)] string CourseName,
    [MaxLength(2000)] string Description,
    int DurationHours,
    [Required] CourseStatus Status,
    int? ValidityMonths = null,
    [MaxLength(50)] string? CourseType = null,
    [Required, MinLength(1, ErrorMessage = "A course must have at least one subject configured upon creation.")] 
    List<AddCourseSubjectRequest> Subjects = null!
);
