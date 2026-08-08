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

    /// <summary>Matches the parent Course.VersionNo that was current when this row became
    /// effective. When RequirementType/ThresholdValue/IsMandatory change, the old row is closed
    /// (EffectiveTo set) and a NEW row is inserted with the bumped VersionNo — see
    /// CompletionRequirementService.UpdateCompletionRequirementAsync. Cosmetic-only edits
    /// (RequirementName/Description/DisplayOrder) update the row in place without versioning.</summary>
    public int VersionNo { get; set; } = 1;

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    /// <summary>Null = still the current version for this CourseId.</summary>
    public DateTime? EffectiveTo { get; set; }
}
