namespace ETR.Application.DTOs;

public record AuditLogResponse(
    long AuditLogId,
    int? AccountId,
    int? ETRRecordId,
    string ActionType,
    string EntityName,
    int RecordId,
    string? OldValue,
    string? NewValue,
    string? Description,
    string? IPAddress,
    string? UserAgent);
