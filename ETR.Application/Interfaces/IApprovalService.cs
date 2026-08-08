using ETR.Application.DTOs;
using ETR.Application.DTOs.Approval;

namespace ETR.Application.Interfaces;

public interface IApprovalService
{
    Task<IEnumerable<ApprovalRequestResponse>> GetAllApprovalRequestsAsync(CancellationToken cancellationToken = default);
    Task<ApprovalRequestResponse> CreateApprovalRequestAsync(int etrCourseRecordId, int? currentApproverId, int submittedByAccountId, CancellationToken cancellationToken = default);
    Task<ApprovalRequestResponse> ProcessApprovalActionAsync(int approvalRequestId, ApprovalActionType action, int actionByAccountId, string? actionByRoleName, string? comment, CancellationToken cancellationToken = default);
    Task<ApprovalRequestResponse> UpdateApprovalRequestAsync(int id, UpdateApprovalRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteApprovalRequestAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
