namespace ETR.Domain.Entities;

public class PracticalChecklistResult : BaseEntity
{
    public int PracticalChecklistResultId { get; set; }
    public int SubjectResultId { get; set; }
    public int PracticalChecklistId { get; set; }
    public bool IsCompleted { get; set; }
    public int? VerifiedByAccountId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? VerificationComment { get; set; }
}
