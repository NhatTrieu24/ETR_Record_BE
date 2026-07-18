namespace ETR.Domain.Entities;

public class ApprovalRequest : BaseEntity
{
    public int ApprovalRequestId { get; set; }
    public int ETRCourseRecordId { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public int SubmittedByAccountId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int? CurrentApproverId { get; set; }
    public DateTime? CompletedAt { get; set; }
}
