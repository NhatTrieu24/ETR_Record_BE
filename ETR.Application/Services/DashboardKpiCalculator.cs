using ETR.Application.DTOs;
using ETR.Application.Interfaces;

namespace ETR.Application.Services;

public record DashboardKpis(
    int TotalEtrs,
    int CompletedCount,
    decimal CompletionRatePercent,
    int PendingApprovalCount,
    int RejectedCount,
    int ReturnedForCorrectionCount,
    int MissingEvidenceCount);

// M9: counts alone don't let a Training Manager actually "đôn đốc" (chase up) anything — they still
// need to click into each list separately to find which ETRs are pending/rejected/missing evidence.
// This carries the same 4 buckets as DashboardKpis, but as ETR ID lists instead of counts.
public record DashboardActionItems(
    IReadOnlyList<int> PendingApprovalEtrIds,
    IReadOnlyList<int> RejectedEtrIds,
    IReadOnlyList<int> ReturnedForCorrectionEtrIds,
    IReadOnlyList<int> MissingEvidenceEtrIds);

/// <summary>Shared KPI computation used by DashboardController.</summary>
public static class DashboardKpiCalculator
{
    public static async Task<DashboardActionItems> ComputeActionItemsAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var etrs = (await unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken)).ToList();
        var approvalRequests = (await unitOfWork.ApprovalRequestRepository.GetAllAsync(cancellationToken)).ToList();
        var evidenceFiles = await unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken);
        var subjectResults = await unitOfWork.SubjectResultRepository.GetAllAsync(cancellationToken);

        var pendingApprovalEtrIds = etrs
            .Where(e => e.Status == "Submitted" || e.Status == "Verified")
            .Select(e => e.ETRCourseRecordId)
            .ToList();

        var rejectedEtrIds = approvalRequests
            .Where(a => a.CurrentStatus == "Rejected")
            .Select(a => a.ETRCourseRecordId)
            .Distinct()
            .ToList();

        var returnedForCorrectionEtrIds = etrs
            .Where(e => e.Status == "ReturnedForCorrection")
            .Select(e => e.ETRCourseRecordId)
            .ToList();

        var etrIdsMissingEvidence = subjectResults
            .Where(sr => !evidenceFiles.Any(e => e.SubjectResultId == sr.SubjectResultId && e.VerificationStatus == "Verified"))
            .Select(sr => sr.EtrId)
            .ToHashSet();
        var missingEvidenceEtrIds = etrs
            .Where(e => e.Status != "Completed" && etrIdsMissingEvidence.Contains(e.ETRCourseRecordId))
            .Select(e => e.ETRCourseRecordId)
            .ToList();

        return new DashboardActionItems(pendingApprovalEtrIds, rejectedEtrIds, returnedForCorrectionEtrIds, missingEvidenceEtrIds);
    }

    // Overview/action-items answer "how much work is left"; this answers "where is it stuck in the
    // pipeline" — a Draft/InProgress-heavy funnel points to an intake problem, a Submitted-heavy one
    // points to a QA bottleneck, etc.
    public static async Task<DashboardStatusFunnel> ComputeStatusFunnelAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var etrs = (await unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken)).ToList();
        int Count(string status) => etrs.Count(e => e.Status == status);

        return new DashboardStatusFunnel(
            Count("Draft"),
            Count("InProgress"),
            Count("Submitted"),
            Count("Verified"),
            Count("Completed"),
            Count("ReturnedForCorrection"),
            Count("Cancelled"));
    }

    public static async Task<DashboardKpis> ComputeAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var etrs = (await unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken)).ToList();
        var approvalRequests = await unitOfWork.ApprovalRequestRepository.GetAllAsync(cancellationToken);
        var evidenceFiles = await unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken);
        var subjectResults = await unitOfWork.SubjectResultRepository.GetAllAsync(cancellationToken);

        var totalEtrs = etrs.Count;
        var completedCount = etrs.Count(e => e.Status == "Completed");
        var pendingApprovalCount = etrs.Count(e => e.Status == "Submitted" || e.Status == "Verified");
        var returnedForCorrectionCount = etrs.Count(e => e.Status == "ReturnedForCorrection");
        var rejectedCount = approvalRequests.Count(a => a.CurrentStatus == "Rejected");

        var etrIdsMissingEvidence = subjectResults
            .Where(sr => !evidenceFiles.Any(e => e.SubjectResultId == sr.SubjectResultId && e.VerificationStatus == "Verified"))
            .Select(sr => sr.EtrId)
            .ToHashSet();
        var missingEvidenceCount = etrs.Count(e => e.Status != "Completed" && etrIdsMissingEvidence.Contains(e.ETRCourseRecordId));

        return new DashboardKpis(
            totalEtrs,
            completedCount,
            totalEtrs > 0 ? Math.Round((decimal)completedCount / totalEtrs * 100, 2) : 0,
            pendingApprovalCount,
            rejectedCount,
            returnedForCorrectionCount,
            missingEvidenceCount);
    }
}
