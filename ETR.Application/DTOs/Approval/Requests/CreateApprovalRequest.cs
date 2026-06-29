namespace ETR.Application.DTOs;

public record CreateApprovalRequest(
    int ETRRecordId,
    int SubmittedBy,
    int? CurrentApproverId);
