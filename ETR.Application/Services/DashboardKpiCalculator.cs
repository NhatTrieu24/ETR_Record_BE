using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Enums;

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
            .Where(e => e.Status == EtrStatus.Submitted || e.Status == EtrStatus.Verified)
            .Select(e => e.ETRCourseRecordId)
            .ToList();

        var rejectedEtrIds = approvalRequests
            .Where(a => a.CurrentStatus == "Rejected")
            .Select(a => a.ETRCourseRecordId)
            .Distinct()
            .ToList();

        var returnedForCorrectionEtrIds = etrs
            .Where(e => e.Status == EtrStatus.ReturnedForCorrection)
            .Select(e => e.ETRCourseRecordId)
            .ToList();

        var etrIdsMissingEvidence = subjectResults
            .Where(sr => !evidenceFiles.Any(e => e.SubjectResultId == sr.SubjectResultId && e.VerificationStatus == "Verified"))
            .Select(sr => sr.EtrId)
            .ToHashSet();
        var missingEvidenceEtrIds = etrs
            .Where(e => e.Status != EtrStatus.Completed && etrIdsMissingEvidence.Contains(e.ETRCourseRecordId))
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
        int Count(EtrStatus status) => etrs.Count(e => e.Status == status);

        return new DashboardStatusFunnel(
            Count(EtrStatus.Draft),
            Count(EtrStatus.InProgress),
            Count(EtrStatus.Submitted),
            Count(EtrStatus.Verified),
            Count(EtrStatus.Completed),
            Count(EtrStatus.ReturnedForCorrection),
            Count(EtrStatus.Cancelled));
    }

    public static async Task<DashboardKpis> ComputeAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var etrs = (await unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken)).ToList();
        var approvalRequests = await unitOfWork.ApprovalRequestRepository.GetAllAsync(cancellationToken);
        var evidenceFiles = await unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken);
        var subjectResults = await unitOfWork.SubjectResultRepository.GetAllAsync(cancellationToken);

        var totalEtrs = etrs.Count;
        var completedCount = etrs.Count(e => e.Status == EtrStatus.Completed);
        var pendingApprovalCount = etrs.Count(e => e.Status == EtrStatus.Submitted || e.Status == EtrStatus.Verified);
        var returnedForCorrectionCount = etrs.Count(e => e.Status == EtrStatus.ReturnedForCorrection);
        var rejectedCount = approvalRequests.Count(a => a.CurrentStatus == "Rejected");

        var etrIdsMissingEvidence = subjectResults
            .Where(sr => !evidenceFiles.Any(e => e.SubjectResultId == sr.SubjectResultId && e.VerificationStatus == "Verified"))
            .Select(sr => sr.EtrId)
            .ToHashSet();
        var missingEvidenceCount = etrs.Count(e => e.Status != EtrStatus.Completed && etrIdsMissingEvidence.Contains(e.ETRCourseRecordId));

        return new DashboardKpis(
            totalEtrs,
            completedCount,
            totalEtrs > 0 ? Math.Round((decimal)completedCount / totalEtrs * 100, 2) : 0,
            pendingApprovalCount,
            rejectedCount,
            returnedForCorrectionCount,
            missingEvidenceCount);
    }

    public static async Task<SystemStatsSummary> ComputeSystemStatsAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var accounts = (await unitOfWork.AccountRepository.GetAllAsync(cancellationToken)).ToList();
        var roles = (await unitOfWork.RoleRepository.GetAllAsync(cancellationToken)).ToDictionary(r => r.RoleId, r => r.RoleName);
        var courses = await unitOfWork.CourseRepository.GetAllAsync(cancellationToken);
        var classes = await unitOfWork.ClassRepository.GetAllAsync(cancellationToken);

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        int CountByRole(string roleName) => accounts.Count(a => roles.GetValueOrDefault(a.RoleId) == roleName);

        return new SystemStatsSummary(
            accounts.Count,
            CountByRole("Student"),
            CountByRole("Instructor"),
            courses.Count(),
            classes.Count(),
            accounts.Count(a => a.IsActive),
            accounts.Count(a => a.CreatedAt >= monthStart));
    }

    // Shared by TrainingManager ("locked vs. returned" chase-up trend) and Auditor (compliance trend)
    // — "locked" = ETRs that became immutable in that month (CompletedAt month, IsLocked); "returned"
    // = ApprovalHistory transitions into ReturnedForCorrection in that month, since ETRCourseRecord
    // itself doesn't stamp when it was last returned.
    public static async Task<MonthlyTrendSummary> ComputeMonthlyTrendAsync(IUnitOfWork unitOfWork, int monthCount, CancellationToken cancellationToken)
    {
        var etrs = await unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        var approvalHistory = await unitOfWork.ApprovalHistoryRepository.GetAllAsync(cancellationToken);

        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthStarts = Enumerable.Range(0, monthCount)
            .Select(i => currentMonth.AddMonths(-(monthCount - 1 - i)))
            .ToList();

        var months = monthStarts.Select(m => m.ToString("yyyy-MM")).ToList();
        var locked = monthStarts
            .Select(m => etrs.Count(e => e.IsLocked && e.CompletedAt.HasValue && e.CompletedAt.Value.Year == m.Year && e.CompletedAt.Value.Month == m.Month))
            .ToList();
        var returned = monthStarts
            .Select(m => approvalHistory.Count(h => h.NewStatus == "ReturnedForCorrection" && h.ActionAt.Year == m.Year && h.ActionAt.Month == m.Month))
            .ToList();

        return new MonthlyTrendSummary(months, locked, returned);
    }

    public static async Task<LockedRecordsSummary> ComputeLockedRecordsSummaryAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var etrs = (await unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken)).ToList();
        var totalLocked = etrs.Count(e => e.IsLocked);
        var complianceRate = etrs.Count > 0 ? Math.Round((decimal)totalLocked / etrs.Count * 100, 2) : 0;
        return new LockedRecordsSummary(totalLocked, complianceRate);
    }

    public static async Task<EvidenceSummary> ComputeEvidenceSummaryAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var evidenceFiles = (await unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken)).ToList();
        return new EvidenceSummary(
            evidenceFiles.Count,
            evidenceFiles.Count(e => e.VerificationStatus == "Verified"),
            evidenceFiles.Count(e => e.VerificationStatus == "Pending"),
            evidenceFiles.Count(e => e.VerificationStatus == "Rejected"));
    }
}
