using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IApprovalService
{
    Task<ApprovalRequestResponse> ProcessApprovalActionAsync(int approvalRequestId, string action, int actionByUserId, string? comment, CancellationToken cancellationToken = default);
}
