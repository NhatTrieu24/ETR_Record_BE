namespace ETR.Domain.Entities;

public class Course : BaseEntity
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ValidityMonths { get; set; }
    public string? CourseType { get; set; }

    /// <summary>Bumped whenever an outcome-affecting field changes (currently: ValidityMonths, or
    /// a linked CompletionRequirement's evaluated fields — see CourseService/CompletionRequirementService).
    /// Snapshotted onto ETRCourseRecord.CourseVersionNo at Enroll time so Checklist Validation always
    /// evaluates a learner against the rules that were in force when they enrolled, not whatever the
    /// rules have since changed to.</summary>
    public int VersionNo { get; set; } = 1;

    /// <summary>When the CURRENT VersionNo took effect.</summary>
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
}
