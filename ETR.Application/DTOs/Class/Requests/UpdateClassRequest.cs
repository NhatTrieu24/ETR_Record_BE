using System.ComponentModel.DataAnnotations;
using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record UpdateClassRequest(
    int ClassId,
    [Required, MaxLength(20)] string ClassCode,
    [Required, MaxLength(200), RegularExpression(@"^[^<>]+$", ErrorMessage = "Invalid characters in ClassName")] string ClassName,
    int CourseId,
    DateTime StartDate,
    DateTime EndDate,
    [MaxLength(200), RegularExpression(@"^[^<>]+$", ErrorMessage = "Invalid characters in Location")] string? Location,
    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1")] int Capacity,
    [Required] ClassStatus Status,
    List<InstructorAssignmentRequest>? InstructorAssignments = null) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
        {
            yield return new ValidationResult("EndDate must be greater than or equal to StartDate.", new[] { nameof(EndDate) });
        }
    }
}
