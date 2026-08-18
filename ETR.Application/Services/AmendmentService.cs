using ETR.Application.Compliance;
using ETR.Application.DTOs.Amendment;
using ETR.Application.DTOs.Amendment.Requests;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ETR.Application.Services;

/// <summary>
/// Lets an Instructor request re-opening a single already-Signed-off SubjectResult for
/// correction, and a Training Manager approve/reject that request — the structured replacement
/// for "call the Manager and ask them to manually walk the whole ETR back" described in
/// concern.md. Deliberately scoped to BEFORE the parent ETR reaches Completed/Locked: once an
/// ETR is Completed, the existing ETR-level reopen flow (EtrService.UnlockEtrAsync) is the correct
/// tool — Amendment does not attempt to also bypass that freeze.
/// </summary>
public class AmendmentService : IAmendmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public AmendmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<AmendmentRequestResponse>> GetAllAmendmentRequestsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _unitOfWork.AmendmentRequestRepository.GetAllAsync(cancellationToken);
        return requests.Select(MapToResponse).ToList();
    }

    public async Task<AmendmentRequestResponse> GetAmendmentRequestByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var request = await _unitOfWork.AmendmentRequestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"AmendmentRequest with ID {id} not found.");
        return MapToResponse(request);
    }

    public async Task<AmendmentRequestResponse> CreateAmendmentRequestAsync(int subjectResultId, CreateAmendmentRequestRequest request, int requestedByAccountId, string? requestedByRoleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException("A reason is required to request an amendment.");

        var subjectResult = await _unitOfWork.SubjectResultRepository.GetByIdAsync(subjectResultId, cancellationToken)
            ?? throw new BusinessRuleViolationException("SubjectResult not found.");

        // Only the most recent signoff matters for identity — if it was already re-signed after a
        // prior amendment, the earlier (soft-deleted) signoffs are excluded by the IsDeleted query
        // filter automatically.
        var currentSignoff = (await _unitOfWork.SubjectSignoffRepository.GetAllAsync(cancellationToken))
            .Where(s => s.SubjectResultId == subjectResultId)
            .OrderByDescending(s => s.SignoffAt)
            .FirstOrDefault();
        if (currentSignoff == null)
            throw new BusinessRuleViolationException("Cannot request an amendment for a SubjectResult that has not been signed off yet — just edit it directly.");

        // "Chỉ chính người đã ký mới được xin mở khóa" (team decision 2026-08-08, docs/todo/addition.md).
        // Admin is the one deliberate exception — a "Force Unlock" for when the original signer has
        // left/is otherwise unavailable — but that path must leave an unmistakably louder audit trail
        // than a normal self-service request (see the AuditLog branch below).
        var isOriginalSigner = currentSignoff.SignoffByAccountId == requestedByAccountId;
        var isAdmin = string.Equals(requestedByRoleName, "Admin", StringComparison.OrdinalIgnoreCase);
        if (!isOriginalSigner && !isAdmin)
        {
            throw new ForbiddenAccessException("Bạn không có quyền can thiệp vào chữ ký của người khác — chỉ người đã Sign-off Subject này mới được xin mở khóa.");
        }
        var isAdminForceUnlock = isAdmin && !isOriginalSigner;

        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(subjectResult.EtrId, cancellationToken);
        if (etr != null && (etr.IsLocked || etr.Status == EtrStatus.Completed))
        {
            throw new BusinessRuleViolationException(
                "The parent ETR is already Completed and locked. Use POST /api/etr/{id}/reopen instead — Amendment requests only apply before the ETR is Completed.");
        }

        var hasPendingRequest = (await _unitOfWork.AmendmentRequestRepository.GetAllAsync(cancellationToken))
            .Any(a => a.SubjectResultId == subjectResultId && a.Status == AmendmentStatus.Pending);
        if (hasPendingRequest)
            throw new BusinessRuleViolationException("An amendment request for this SubjectResult is already Pending.");

        var amendment = new AmendmentRequest
        {
            SubjectResultId = subjectResultId,
            RequestedByAccountId = requestedByAccountId,
            Reason = request.Reason,
            OldValue = subjectResult.Status.ToString(),
            Status = AmendmentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = requestedByAccountId
        };

        await _unitOfWork.AmendmentRequestRepository.AddAsync(amendment, cancellationToken);

        // Admin Force Unlock gets its OWN ActionType and an unmissable description — this must never
        // be filed under the same "AMENDMENT_REQUEST" bucket as an ordinary self-service request, or
        // an Auditor scanning the log for signer-identity mismatches would have no way to find it.
        await _unitOfWork.AuditLogRepository.AddAsync(isAdminForceUnlock
            ? new AuditLog
            {
                AccountId = requestedByAccountId,
                ActionType = AuditActionType.ADMIN_FORCE_UNLOCK.ToString(),
                EntityName = nameof(SubjectResult),
                RecordId = subjectResultId,
                OldValue = subjectResult.Status.ToString(),
                NewValue = "Pending amendment",
                Description = $"[CẢNH BÁO] Admin (AccountId {requestedByAccountId}) can thiệp phá vỡ chữ ký của người khác — Force Unlock SubjectResult #{subjectResultId} vốn được ký bởi AccountId {currentSignoff.SignoffByAccountId}. Reason: {request.Reason}"
            }
            : new AuditLog
            {
                AccountId = requestedByAccountId,
                ActionType = AuditActionType.AMENDMENT_REQUEST.ToString(),
                EntityName = nameof(SubjectResult),
                RecordId = subjectResultId,
                OldValue = subjectResult.Status.ToString(),
                NewValue = "Pending amendment",
                Description = $"Amendment requested for SubjectResult #{subjectResultId}. Reason: {request.Reason}"
            }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(amendment);
    }

    public async Task<AmendmentRequestResponse> ApproveAmendmentRequestAsync(int id, DecideAmendmentRequestRequest request, int approvedByAccountId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var amendment = await GetPendingOrThrowAsync(id, ct);

                var subjectResult = await _unitOfWork.SubjectResultRepository.GetByIdAsync(amendment.SubjectResultId, ct)
                    ?? throw new BusinessRuleViolationException("SubjectResult not found.");

                // Re-check at decision time, not just at request time — the parent ETR could have
                // been Completed by someone else while this request sat Pending.
                var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(subjectResult.EtrId, ct);
                if (etr != null && (etr.IsLocked || etr.Status == EtrStatus.Completed))
                {
                    throw new BusinessRuleViolationException(
                        "The parent ETR has since become Completed and locked. Reopen it first via POST /api/etr/{id}/reopen before approving this amendment.");
                }

                // Reset to the same status a freshly-created SubjectResult starts at (see
                // EnrollmentService.CreateEnrollmentAsync) — the Instructor corrects the underlying
                // data and signs off again, re-running the normal Passed/Failed evaluation.
                const SubjectResultStatus reopenedStatus = SubjectResultStatus.Pending;
                subjectResult.Status = reopenedStatus;
                subjectResult.UpdatedAt = DateTime.UtcNow;
                subjectResult.UpdatedByAccountId = approvedByAccountId;
                _unitOfWork.SubjectResultRepository.Update(subjectResult);

                // Soft-delete the existing signoff(s) so EtrService.SubmitEtrAsync's "has this
                // subject been signed off" check correctly requires a FRESH signoff before this ETR
                // can be submitted again — otherwise the old (now-incorrect) signoff would still count.
                var signoffs = (await _unitOfWork.SubjectSignoffRepository.GetAllAsync(ct))
                    .Where(s => s.SubjectResultId == amendment.SubjectResultId)
                    .ToList();
                foreach (var signoff in signoffs)
                {
                    signoff.IsDeleted = true;
                    signoff.DeletedAt = DateTime.UtcNow;
                    signoff.UpdatedAt = DateTime.UtcNow;
                    signoff.UpdatedByAccountId = approvedByAccountId;
                    _unitOfWork.SubjectSignoffRepository.Update(signoff);
                }

                amendment.Status = AmendmentStatus.Approved;
                amendment.NewValue = reopenedStatus.ToString();
                amendment.ApprovedByAccountId = approvedByAccountId;
                amendment.ApprovedAt = DateTime.UtcNow;
                amendment.DecisionComment = request.Comment;
                amendment.UpdatedAt = DateTime.UtcNow;
                amendment.UpdatedByAccountId = approvedByAccountId;
                _unitOfWork.AmendmentRequestRepository.Update(amendment);

                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = approvedByAccountId,
                    ActionType = AuditActionType.AMENDMENT_APPROVE.ToString(),
                    EntityName = nameof(SubjectResult),
                    RecordId = amendment.SubjectResultId,
                    OldValue = amendment.OldValue,
                    NewValue = reopenedStatus.ToString(),
                    Description = $"Amendment request #{id} approved — SubjectResult #{amendment.SubjectResultId} reopened for correction, {signoffs.Count} prior signoff(s) invalidated."
                }, ct);

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return MapToResponse(amendment);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<AmendmentRequestResponse> RejectAmendmentRequestAsync(int id, DecideAmendmentRequestRequest request, int rejectedByAccountId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Comment))
            throw new ValidationException("A comment is required when rejecting an amendment request.");

        var amendment = await GetPendingOrThrowAsync(id, cancellationToken);

        amendment.Status = AmendmentStatus.Rejected;
        amendment.ApprovedByAccountId = rejectedByAccountId;
        amendment.ApprovedAt = DateTime.UtcNow;
        amendment.DecisionComment = request.Comment;
        amendment.UpdatedAt = DateTime.UtcNow;
        amendment.UpdatedByAccountId = rejectedByAccountId;
        _unitOfWork.AmendmentRequestRepository.Update(amendment);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = rejectedByAccountId,
            ActionType = AuditActionType.AMENDMENT_REJECT.ToString(),
            EntityName = nameof(SubjectResult),
            RecordId = amendment.SubjectResultId,
            OldValue = amendment.OldValue,
            NewValue = amendment.OldValue,
            Description = $"Amendment request #{id} rejected for SubjectResult #{amendment.SubjectResultId}. Comment: {request.Comment}"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(amendment);
    }

    private async Task<AmendmentRequest> GetPendingOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var amendment = await _unitOfWork.AmendmentRequestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"AmendmentRequest with ID {id} not found.");

        if (amendment.Status != AmendmentStatus.Pending)
            throw new BusinessRuleViolationException($"AmendmentRequest #{id} has already been {amendment.Status.ToString().ToLowerInvariant()}.");

        return amendment;
    }

    private static AmendmentRequestResponse MapToResponse(AmendmentRequest a) => new(
        a.AmendmentRequestId,
        a.SubjectResultId,
        a.RequestedByAccountId,
        a.Reason,
        a.OldValue,
        a.NewValue,
        a.Status,
        a.ApprovedByAccountId,
        a.ApprovedAt,
        a.DecisionComment,
        a.CreatedAt);
}
