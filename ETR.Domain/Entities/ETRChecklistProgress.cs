namespace ETR.Domain.Entities;

public class ETRChecklistProgress : BaseEntity
{
    public int ProgressId { get; set; }
    public int ETRRecordId { get; set; }
    public int ChecklistItemId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? VerifiedBy { get; set; }
    public string? VerificationComment { get; set; }
}
