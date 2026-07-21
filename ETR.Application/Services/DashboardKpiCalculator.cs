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

/// <summary>Shared KPI computation used by both DashboardController and ReportsController.</summary>
public static class DashboardKpiCalculator
{
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
