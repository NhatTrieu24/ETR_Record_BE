using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IApprovalService
{
    Task<IEnumerable<ApprovalRequestResponse>> GetAllApprovalRequestsAsync(CancellationToken cancellationToken = default);
    Task<ApprovalRequestResponse> CreateApprovalRequestAsync(int etrCourseRecordId, int? currentApproverId, int submittedByAccountId, CancellationToken cancellationToken = default);
    Task<ApprovalRequestResponse> ProcessApprovalActionAsync(int approvalRequestId, string action, int actionByAccountId, string? comment, CancellationToken cancellationToken = default);
}
