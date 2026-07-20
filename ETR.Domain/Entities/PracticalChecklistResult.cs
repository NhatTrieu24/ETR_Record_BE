namespace ETR.Domain.Entities;

public class PracticalChecklistResult : BaseEntity
{
    public int PracticalChecklistResultId { get; set; }
    public int? SessionId { get; set; }
    public int SubjectResultId { get; set; }
    public int PracticalChecklistId { get; set; }
    public decimal Score { get; set; }
    public string ResultStatus { get; set; } = string.Empty;
    public int? VerifiedByAccountId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? VerificationComment { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
}
