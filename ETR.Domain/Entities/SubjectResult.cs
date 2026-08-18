using ETR.Domain.Enums;

namespace ETR.Domain.Entities;

public class SubjectResult : BaseEntity
{
    public int SubjectResultId { get; set; }
    public int EtrId { get; set; }
    public int CourseId { get; set; }
    public int SubjectId { get; set; }
    public decimal? AttendanceRate { get; set; }
    public decimal? Score { get; set; }
    public SubjectResultStatus Status { get; set; }
    public int? EvaluatedByAccountId { get; set; }
    public DateTime? EvaluatedAt { get; set; }

    /// <summary>Snapshot of CourseSubject.PassingScore at Enroll time. CourseSubject has a composite
    /// PK (CourseId, SubjectId) with no surrogate id, so it cannot be versioned the same append-only
    /// way as Course/CompletionRequirement — this snapshot is the lighter-weight equivalent: it lets
    /// AssessmentResultService.EvaluateSubjectPassabilityAsync grade against the threshold that was
    /// in force when the learner enrolled, even if CourseSubject.PassingScore changes afterwards.
    /// Null for records created before this field existed — those fall back to the live value.</summary>
    public decimal? PassingScoreSnapshot { get; set; }
}
