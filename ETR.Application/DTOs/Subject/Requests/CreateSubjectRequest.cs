using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record CreateSubjectRequest(
    [Required, MaxLength(20)] string SubjectCode,
    [Required, MaxLength(200)] string SubjectName,
    [Required, MaxLength(50)] string SubjectType,
    int DefaultHours,
    [MaxLength(100)] string? AssessmentMethod,
    [MaxLength(2000)] string? Description,
    [Required, MaxLength(20)] string Status);
