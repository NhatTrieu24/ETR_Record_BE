namespace ETR.Application.DTOs;

public record CreateApprovalRequest(
    int ETRCourseRecordId,
    int SubmittedBy,
    int? CurrentApproverId);
