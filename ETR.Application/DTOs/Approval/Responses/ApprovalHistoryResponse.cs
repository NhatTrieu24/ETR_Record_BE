namespace ETR.Application.DTOs;

public record ApprovalHistoryResponse(
    int ApprovalHistoryId,
    int ApprovalRequestId,
    int ActionBy,
    string ActionType,
    string? PreviousStatus,
    string? NewStatus,
    string? Comments,
    DateTime ActionAt);
