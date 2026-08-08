namespace ETR.Domain.Entities;

public class AssessmentResult : BaseEntity
{
    public int AssessmentResultId { get; set; }
    public int AssessmentId { get; set; }
    public int AccountId { get; set; }
    public int SubjectResultId { get; set; }
    public int? SessionId { get; set; }
    public decimal Score { get; set; }
    public string ResultStatus { get; set; } = string.Empty;
    public int GradedByAccountId { get; set; }
    public DateTime RecordedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? TakenAt { get; set; }
    public string? Remark { get; set; }
    public int AttemptNo { get; set; } = 1;

    /// <summary>Snapshot of Assessment.PassingScore captured when this attempt was first recorded.
    /// Assessment has no versioning of its own (unlike Course/CompletionRequirement), so this
    /// snapshot is what keeps Pass/Fail grading stable if Assessment.PassingScore is edited after
    /// students have already started attempting it. Retake attempts (AttemptNo > 1) inherit the
    /// SAME snapshot as attempt 1 — see AssessmentResultService.RecordAssessmentScoreAsync. Null for
    /// records created before this field existed — those fall back to the live value.</summary>
    public decimal? PassingScoreSnapshot { get; set; }

    /// <summary>Snapshot of Assessment.Weight at the same moment PassingScoreSnapshot is captured —
    /// AssessmentResultService.CalculateSubjectResultScoreAsync uses it to compute the weighted
    /// average instead of a live Assessment.Weight, for the same retroactive-recompute reason as
    /// PassingScoreSnapshot. Null for records created before this field existed.</summary>
    public decimal? WeightSnapshot { get; set; }

    // Navigation
    public Session? Session { get; set; }
}
