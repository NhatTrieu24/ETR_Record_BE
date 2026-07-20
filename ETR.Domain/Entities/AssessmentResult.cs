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

    // Navigation
    public Session? Session { get; set; }
}
