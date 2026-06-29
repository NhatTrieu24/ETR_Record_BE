namespace ETR.Domain.Entities;

public class AuditLog
{
    public long AuditLogId { get; set; }
    public int? UserId { get; set; }
    public int? ETRRecordId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int RecordId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Description { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
}
