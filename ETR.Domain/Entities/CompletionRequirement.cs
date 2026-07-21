namespace ETR.Domain.Entities;

public class CompletionRequirement : BaseEntity
{
    public int RequirementId { get; set; }
    public int CourseId { get; set; }
    public string RequirementName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsMandatory { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Machine-evaluated requirement kind — one of "MinAttendance", "AllAssessmentsPassed",
    /// "AllChecklistsSignedOff", or null for a free-text/advisory requirement (not enforced
    /// by <c>EtrService.SubmitEtrAsync</c>).
    /// </summary>
    public string? RequirementType { get; set; }

    /// <summary>Threshold used by "MinAttendance" (percentage). Unused by other types.</summary>
    public decimal? ThresholdValue { get; set; }
}
