using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace ETR.Application.Services;

public class ApprovalService : IApprovalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEtrService _etrService;

    public ApprovalService(IUnitOfWork unitOfWork, IEtrService etrService)
    {
        _unitOfWork = unitOfWork;
        _etrService = etrService;
    }

    public async Task<IEnumerable<ApprovalRequestResponse>> GetAllApprovalRequestsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _unitOfWork.ApprovalRequestRepository.GetAllAsync(cancellationToken);
        return requests.Select(r => new ApprovalRequestResponse(r.ApprovalRequestId, r.ETRCourseRecordId, r.CurrentStatus, r.SubmittedByAccountId, r.SubmittedAt, r.CurrentApproverId, r.CompletedAt));
    }

    public async Task<ApprovalRequestResponse> CreateApprovalRequestAsync(int etrCourseRecordId, int? currentApproverId, int submittedByAccountId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(etrCourseRecordId, cancellationToken);
        if (etr == null) throw new BusinessRuleViolationException("ETRCourseRecord not found.");

        var request = new ApprovalRequest
        {
            ETRCourseRecordId = etrCourseRecordId,
            CurrentStatus = "Pending",
            SubmittedByAccountId = submittedByAccountId,
            SubmittedAt = DateTime.UtcNow,
            CurrentApproverId = currentApproverId,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = submittedByAccountId
        };

        await _unitOfWork.ApprovalRequestRepository.AddAsync(request, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new ApprovalRequestResponse(request.ApprovalRequestId, request.ETRCourseRecordId, request.CurrentStatus, request.SubmittedByAccountId, request.SubmittedAt, request.CurrentApproverId, request.CompletedAt);
    }

    public async Task<ApprovalRequestResponse> ProcessApprovalActionAsync(int approvalRequestId, string action, int actionByAccountId, string? comment, CancellationToken cancellationToken = default)
    {
        if ((action == "Reject" || action == "Return") && string.IsNullOrWhiteSpace(comment))
        {
            throw new ValidationException("A comment is required when rejecting or returning an ApprovalRequest.");
        }

        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var request = await _unitOfWork.ApprovalRequestRepository.GetByIdAsync(approvalRequestId, ct);
                if (request == null) throw new BusinessRuleViolationException("ApprovalRequest not found.");

                var prevStatus = request.CurrentStatus;
                string newStatus = action switch
                {
                    "Approve" => "Approved",
                    "Reject" => "Rejected",
                    "Verify" => "Verified",
                    "Return" => "ReturnedForCorrection",
                    _ => throw new BusinessRuleViolationException("Invalid action.")
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

                await _unitOfWork.SaveAsync(ct);

                if (newStatus == "Approved")
                {
                    await _etrService.CompleteEtrAsync(request.ETRCourseRecordId, actionByAccountId, ct);
                }

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

    public async Task<ApprovalRequestResponse> UpdateApprovalRequestAsync(int id, UpdateApprovalRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.ApprovalRequestRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("ApprovalRequest not found.");

        if (item.CurrentStatus != "Pending")
            throw new BusinessRuleViolationException("Cannot update an ApprovalRequest that is not in Pending status.");

        item.CurrentApproverId = request.CurrentApproverId;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.ApprovalRequestRepository.Update(item);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new ApprovalRequestResponse(item.ApprovalRequestId, item.ETRCourseRecordId, item.CurrentStatus, item.SubmittedByAccountId, item.SubmittedAt, item.CurrentApproverId, item.CompletedAt);
    }

    public async Task DeleteApprovalRequestAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.ApprovalRequestRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("ApprovalRequest not found.");

        if (item.CurrentStatus != "Pending")
            throw new BusinessRuleViolationException("Cannot delete an ApprovalRequest that is not in Pending status.");

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.ApprovalRequestRepository.Update(item);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
