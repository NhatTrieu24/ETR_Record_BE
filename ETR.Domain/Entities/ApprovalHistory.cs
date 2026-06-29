namespace ETR.Domain.Entities;

public class ApprovalHistory : BaseEntity
{
    public int ApprovalHistoryId { get; set; }
    public int ApprovalRequestId { get; set; }
    public int ActionBy { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
    public string? Comments { get; set; }
    public DateTime ActionAt { get; set; }
}
