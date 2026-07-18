using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IApprovalService
{
    Task<ApprovalRequestResponse> ProcessApprovalActionAsync(int approvalRequestId, string action, int actionByAccountId, string? comment, CancellationToken cancellationToken = default);
}
