using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.DTOs.Approval;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
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

    // Which role is allowed to perform each workflow action — enforced here (not just via the
    // controller's [Authorize]) because the controller grants a broad role set to cover all 4
    // actions at once; without this, any of those roles could perform any action (e.g. Instructor
    // self-approving their own submission), defeating the QA/TrainingManager segregation of duties.
    private static readonly Dictionary<ApprovalActionType, string[]> AllowedRolesByAction = new()
    {
        [ApprovalActionType.Verify] = ["QA", "Admin"],
        [ApprovalActionType.Approve] = ["TrainingManager", "Admin"],
        [ApprovalActionType.Reject] = ["QA", "Admin"],
        [ApprovalActionType.Return] = ["QA", "Admin"],
    };

    // ApprovalActionType (API/DTO contract for ?action=) is a deliberately separate enum from
    // ApprovalHistoryActionType/AuditActionType (persisted history taxonomies) — this maps between
    // them instead of relying on ToString()/ToUpperInvariant() string transforms, so a rename on
    // either side fails to compile instead of silently producing a mismatched persisted value.
    private static readonly Dictionary<ApprovalActionType, ApprovalHistoryActionType> HistoryActionByApprovalAction = new()
    {
        [ApprovalActionType.Verify] = ApprovalHistoryActionType.Verify,
        [ApprovalActionType.Approve] = ApprovalHistoryActionType.Approve,
        [ApprovalActionType.Reject] = ApprovalHistoryActionType.Reject,
        [ApprovalActionType.Return] = ApprovalHistoryActionType.Return,
    };

    private static readonly Dictionary<ApprovalActionType, AuditActionType> AuditActionByApprovalAction = new()
    {
        [ApprovalActionType.Verify] = AuditActionType.VERIFY,
        [ApprovalActionType.Approve] = AuditActionType.APPROVE,
        [ApprovalActionType.Reject] = AuditActionType.REJECT,
        [ApprovalActionType.Return] = AuditActionType.RETURN,
    };

    public async Task<ApprovalRequestResponse> ProcessApprovalActionAsync(int approvalRequestId, ApprovalActionType action, int actionByAccountId, string? actionByRoleName, string? comment, CancellationToken cancellationToken = default)
    {
        // action is a real enum now (see ApprovalActionType) — ASP.NET Core model binding already
        // rejects any value outside the 4 known members with a 400 before this method is even
        // called, so no "unknown action" branch is needed here anymore.
        var allowedRoles = AllowedRolesByAction[action];

        if (actionByRoleName == null || !allowedRoles.Contains(actionByRoleName))
        {
            throw new ForbiddenAccessException($"Role '{actionByRoleName}' is not authorized to perform the '{action}' action.");
        }

        if ((action == ApprovalActionType.Reject || action == ApprovalActionType.Return) && string.IsNullOrWhiteSpace(comment))
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
                    ApprovalActionType.Approve => "Approved",
                    ApprovalActionType.Reject => "Rejected",
                    ApprovalActionType.Verify => "Verified",
                    ApprovalActionType.Return => "ReturnedForCorrection",
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
                    ActionType = HistoryActionByApprovalAction[action].ToString(),
                    PreviousStatus = prevStatus,
                    NewStatus = newStatus,
                    Comments = comment,
                    ActionAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAccountId = actionByAccountId
                };

                await _unitOfWork.ApprovalHistoryRepository.AddAsync(history, ct);

                // Every ApprovalRequest transition also lands in the main AuditLog — previously only
                // ApprovalHistory captured this, leaving the system-wide audit trail (and the CAA
                // export's Audit_History.pdf, which reads ApprovalHistory but not AuditLog) blind to
                // who acted and why outside this one table.
                var auditLog = new AuditLog
                {
                    ETRRecordId = request.ETRCourseRecordId,
                    AccountId = actionByAccountId,
                    ActionType = AuditActionByApprovalAction[action].ToString(),
                    EntityName = nameof(ApprovalRequest),
                    RecordId = request.ApprovalRequestId,
                    OldValue = prevStatus,
                    NewValue = newStatus,
                    Description = $"ApprovalRequest #{request.ApprovalRequestId} for ETR #{request.ETRCourseRecordId}: {action}. Comment: {comment ?? "N/A"}"
                };
                await _unitOfWork.AuditLogRepository.AddAsync(auditLog, ct);

                // Reject/Return must also push the underlying ETR back to "ReturnedForCorrection" —
                // previously only "Approve" touched ETRCourseRecord.Status, so a QA rejection left the
                // ETR stuck at "Submitted" forever with no visible outcome to Academic Staff. There is
                // no separate terminal "Rejected" status on ETRCourseRecord (only ApprovalRequest.
                // CurrentStatus distinguishes Rejected from ReturnedForCorrection, which is what the
                // Dashboard's RejectedCount already reads) — both actions send the record back for the
                // same practical next step: correct and resubmit.
                if (newStatus == "Rejected" || newStatus == "ReturnedForCorrection")
                {
                    var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(request.ETRCourseRecordId, ct)
                        ?? throw new BusinessRuleViolationException("ETRCourseRecord not found.");

                    if (etr.Status == "Submitted")
                    {
                        etr.Status = "ReturnedForCorrection";
                        etr.UpdatedAt = DateTime.UtcNow;
                        etr.UpdatedByAccountId = actionByAccountId;
                        _unitOfWork.ETRCourseRecordRepository.Update(etr);
                    }
                }

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
