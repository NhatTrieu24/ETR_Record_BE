namespace ETR.Application.Compliance;

/// <summary>Valid values for <see cref="ETR.Domain.Entities.UserProfile.Status"/>. The field itself
/// stays a free string (no DB-level enum) to match the rest of the codebase, but every place that
/// sets it should reference these constants instead of a magic string.</summary>
public static class LearnerStatus
{
    public const string Active = "Active";
    public const string Withdrawn = "Withdrawn";
    public const string Graduated = "Graduated";

    /// <summary>Learner has at least one Course whose most recently issued ETRCourseRecord has
    /// expired (ExpiryDate in the past) with no newer enrollment/ETR for that same Course yet —
    /// aviation-specific "not currently qualified for duty" status. Set/cleared automatically by
    /// <see cref="CertificateValidityCalculator"/> consumers (see EtrService.RefreshGroundedStatusAsync
    /// and EnrollmentService.CreateEnrollmentAsync); never set by a plain profile edit.</summary>
    public const string Grounded = "Grounded";
}
