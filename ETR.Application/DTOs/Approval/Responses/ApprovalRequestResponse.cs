namespace ETR.Application.DTOs;

public record ApprovalRequestResponse(
    int ApprovalRequestId,
    int ETRRecordId,
    string CurrentStatus,
    int SubmittedBy,
    DateTime SubmittedAt,
    int? CurrentApproverId,
    DateTime? CompletedAt);
