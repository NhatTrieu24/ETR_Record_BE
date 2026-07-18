using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class ApprovalService : IApprovalService
{
    private readonly IUnitOfWork _unitOfWork;

    public ApprovalService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApprovalRequestResponse> ProcessApprovalActionAsync(int approvalRequestId, string action, int actionByAccountId, string? comment, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var request = await _unitOfWork.ApprovalRequestRepository.GetByIdAsync(approvalRequestId, ct);
                if (request == null) throw new InvalidOperationException("ApprovalRequest not found.");

                var prevStatus = request.CurrentStatus;
                string newStatus = action switch
                {
                    "Approve" => "Approved",
                    "Reject" => "Rejected",
                    "Verify" => "Verified",
                    "Return" => "ReturnedForCorrection",
                    _ => throw new InvalidOperationException("Invalid action.")
                };

                request.CurrentStatus = newStatus;
                request.UpdatedAt = DateTime.UtcNow;
                request.UpdatedByAccountId = actionByAccountId;
                if (newStatus == "Approved" || newStatus == "Rejected")
                {
                    request.CompletedAt = DateTime.UtcNow;
                }

                _unitOfWork.ApprovalRequestRepository.Update(request);
                
                var history = new ApprovalHistory
                {
                    ApprovalRequestId = request.ApprovalRequestId,
                    ActionByAccountId = actionByAccountId,
                    ActionType = action,
                    PreviousStatus = prevStatus,
                    NewStatus = newStatus,
                    Comments = comment,
                    ActionAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAccountId = actionByAccountId
                };
                
                await _unitOfWork.ApprovalHistoryRepository.AddAsync(history, ct);

                if (newStatus == "Approved")
                {
                    var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(request.ETRCourseRecordId, ct);
                    if (etr != null)
                    {
                        etr.Status = "Completed";
                        etr.CompletedAt = DateTime.UtcNow;
                        etr.IsLocked = true;
                        _unitOfWork.ETRCourseRecordRepository.Update(etr);
                    }
                }

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new ApprovalRequestResponse(request.ApprovalRequestId, request.ETRCourseRecordId, request.CurrentStatus, request.SubmittedByAccountId, request.SubmittedAt, request.CurrentApproverId, request.CompletedAt);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }
}
