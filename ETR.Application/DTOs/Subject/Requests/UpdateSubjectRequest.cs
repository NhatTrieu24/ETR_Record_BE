using System.ComponentModel.DataAnnotations;
using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record UpdateSubjectRequest(
    int SubjectId,
    [Required, MaxLength(20)] string SubjectCode,
    [Required, MaxLength(200)] string SubjectName,
    [Required, MaxLength(50)] string SubjectType,
    int DefaultHours,
    [MaxLength(100)] string? AssessmentMethod,
    [MaxLength(2000)] string? Description,
    [Required, Range(1, int.MaxValue)] int MinSessions,
    [Required, Range(1, int.MaxValue)] int MaxSessions,
    [Required] SubjectStatus Status) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinSessions > MaxSessions)
        {
            yield return new ValidationResult("MinSessions cannot be greater than MaxSessions.", new[] { nameof(MinSessions), nameof(MaxSessions) });
        }
    }
}
