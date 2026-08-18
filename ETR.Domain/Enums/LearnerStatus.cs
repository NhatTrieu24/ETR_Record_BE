namespace ETR.Domain.Enums;

/// <summary>Every value ever written to UserProfile.Status. Column stays nvarchar in the DB
/// (HasConversion&lt;string&gt; in AppDbContext). Replaces the former
/// <c>ETR.Application.Compliance.LearnerStatus</c> string-constants class with a real enum.</summary>
public enum LearnerStatus
{
    Active,
    Withdrawn,
    Graduated,

    /// <summary>Learner has at least one Course whose most recently issued ETRCourseRecord has
    /// expired (ExpiryDate in the past) with no newer enrollment/ETR for that same Course yet —
    /// aviation-specific "not currently qualified for duty" status. Set/cleared automatically by
    /// <c>CertificateValidityCalculator</c> consumers (see EtrService.RefreshGroundedStatusAsync
    /// and EnrollmentService.CreateEnrollmentAsync); never set by a plain profile edit.</summary>
    Grounded
}
