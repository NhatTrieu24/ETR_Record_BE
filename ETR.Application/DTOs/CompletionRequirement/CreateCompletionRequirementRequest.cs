using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.CompletionRequirement;

public class CreateCompletionRequirementRequest
{
    [Required]
    public int CourseId { get; set; }

    [Required]
    public string RequirementName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsMandatory { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>"MinAttendance" | "AllAssessmentsPassed" | "AllChecklistsSignedOff" | null (advisory-only).</summary>
    public string? RequirementType { get; set; }

    /// <summary>Threshold used by "MinAttendance" (percentage 0-100) or "MinFlightHours"/"MinSimulatorHours" (hours 0-999.99).</summary>
    [Range(0, 999.99, ErrorMessage = "ThresholdValue must be between 0 and 999.99")]
    public decimal? ThresholdValue { get; set; }
}
