using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;

namespace ETR.Application.Services;

// Role-aware "my dashboard" composition root — deliberately does NOT duplicate any KPI/scoping
// logic already owned elsewhere (DashboardKpiCalculator, AttendanceService.GetLowAttendanceStudentsAsync,
// EtrService.GetCompletionProgressAsync); it only decides WHICH of those to call based on the
// caller's role and assembles the results into one payload.
public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAttendanceService _attendanceService;
    private readonly IEtrService _etrService;

    public DashboardService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IAttendanceService attendanceService,
        IEtrService etrService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _attendanceService = attendanceService;
        _etrService = etrService;
    }

    public async Task<MyDashboardResponse> GetMyDashboardAsync(CancellationToken cancellationToken = default)
    {
        var role = _currentUserService.RoleName ?? string.Empty;
        var accountId = _currentUserService.AccountId;

        DashboardKpis? overview = null;
        DashboardStatusFunnel? statusFunnel = null;
        DashboardActionItems? actionItems = null;
        IEnumerable<InstructorClassSummary>? myClasses = null;
        IEnumerable<LowAttendanceStudentResponse>? lowAttendanceStudents = null;
        IEnumerable<int>? pendingVerificationEtrIds = null;
        IEnumerable<StudentEtrSummary>? myEtrs = null;

        SystemStatsSummary? systemStats = null;
        MonthlyTrendSummary? monthlyTrend = null;
        int? expiringStudentsCount = null;
        LockedRecordsSummary? lockedRecords = null;
        IEnumerable<RecentLockedEtrSummary>? recentLockedEtrs = null;
        IEnumerable<AuditLogResponse>? recentAuditLogs = null;
        IEnumerable<ExportJobResponse>? recentExportJobs = null;
        EvidenceSummary? evidenceSummary = null;
        int? reviewedToday = null;
        IEnumerable<RecentEvidenceFileSummary>? recentEvidenceFiles = null;
        IEnumerable<SessionSummary>? todaySessions = null;
        int? pendingSignoffs = null;
        StudentProfileSummary? profile = null;
        CertificateSummary? certificateSummary = null;

        switch (role)
        {
            case "Admin":
                overview = await DashboardKpiCalculator.ComputeAsync(_unitOfWork, cancellationToken);
                statusFunnel = await DashboardKpiCalculator.ComputeStatusFunnelAsync(_unitOfWork, cancellationToken);
                actionItems = await DashboardKpiCalculator.ComputeActionItemsAsync(_unitOfWork, cancellationToken);
                systemStats = await DashboardKpiCalculator.ComputeSystemStatsAsync(_unitOfWork, cancellationToken);
                break;

            case "TrainingManager":
                overview = await DashboardKpiCalculator.ComputeAsync(_unitOfWork, cancellationToken);
                statusFunnel = await DashboardKpiCalculator.ComputeStatusFunnelAsync(_unitOfWork, cancellationToken);
                actionItems = await DashboardKpiCalculator.ComputeActionItemsAsync(_unitOfWork, cancellationToken);
                monthlyTrend = await DashboardKpiCalculator.ComputeMonthlyTrendAsync(_unitOfWork, MonthlyTrendMonthCount, cancellationToken);
                break;

            case "Academic":
                overview = await DashboardKpiCalculator.ComputeAsync(_unitOfWork, cancellationToken);
                statusFunnel = await DashboardKpiCalculator.ComputeStatusFunnelAsync(_unitOfWork, cancellationToken);
                actionItems = await DashboardKpiCalculator.ComputeActionItemsAsync(_unitOfWork, cancellationToken);
                lowAttendanceStudents = await _attendanceService.GetLowAttendanceStudentsAsync(null, cancellationToken);
                var dueForTraining = await _etrService.GetDueForTrainingAsync(null, ExpiringSoonDaysThreshold, cancellationToken);
                expiringStudentsCount = dueForTraining.Count(s => s.ValidityStatus == "ExpiringSoon");
                break;

            case "ManagementViewer":
                overview = await DashboardKpiCalculator.ComputeAsync(_unitOfWork, cancellationToken);
                statusFunnel = await DashboardKpiCalculator.ComputeStatusFunnelAsync(_unitOfWork, cancellationToken);
                actionItems = await DashboardKpiCalculator.ComputeActionItemsAsync(_unitOfWork, cancellationToken);
                break;

            case "Audit":
                overview = await DashboardKpiCalculator.ComputeAsync(_unitOfWork, cancellationToken);
                statusFunnel = await DashboardKpiCalculator.ComputeStatusFunnelAsync(_unitOfWork, cancellationToken);
                actionItems = await DashboardKpiCalculator.ComputeActionItemsAsync(_unitOfWork, cancellationToken);
                lockedRecords = await DashboardKpiCalculator.ComputeLockedRecordsSummaryAsync(_unitOfWork, cancellationToken);
                monthlyTrend = await DashboardKpiCalculator.ComputeMonthlyTrendAsync(_unitOfWork, MonthlyTrendMonthCount, cancellationToken);
                recentLockedEtrs = await ComputeRecentLockedEtrsAsync(cancellationToken);
                recentAuditLogs = await ComputeRecentAuditLogsAsync(cancellationToken);
                recentExportJobs = await ComputeRecentExportJobsAsync(cancellationToken);
                break;

            case "Instructor":
                if (accountId.HasValue)
                {
                    myClasses = await ComputeInstructorClassesAsync(accountId.Value, cancellationToken);

                    var allLowAttendance = await _attendanceService.GetLowAttendanceStudentsAsync(null, cancellationToken);
                    var myClassIds = myClasses.Select(c => c.ClassId).ToHashSet();
                    lowAttendanceStudents = allLowAttendance.Where(s => myClassIds.Contains(s.ClassId)).ToList();

                    todaySessions = await ComputeTodaySessionsAsync(myClassIds, cancellationToken);
                    pendingSignoffs = await ComputePendingSignoffsCountAsync(accountId.Value, cancellationToken);
                }
                break;

            case "QA":
                var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
                pendingVerificationEtrIds = etrs.Where(e => e.Status == EtrStatus.Submitted).Select(e => e.ETRCourseRecordId).ToList();
                actionItems = await DashboardKpiCalculator.ComputeActionItemsAsync(_unitOfWork, cancellationToken);
                evidenceSummary = await DashboardKpiCalculator.ComputeEvidenceSummaryAsync(_unitOfWork, cancellationToken);
                reviewedToday = await ComputeReviewedTodayCountAsync(cancellationToken);
                recentEvidenceFiles = await ComputeRecentEvidenceFilesAsync(cancellationToken);
                break;

            case "Student":
                if (accountId.HasValue)
                {
                    myEtrs = await ComputeMyEtrsAsync(accountId.Value, cancellationToken);
                    profile = await ComputeStudentProfileAsync(accountId.Value, cancellationToken);
                    certificateSummary = ComputeCertificateSummary(myEtrs);
                }
                break;
        }

        return new MyDashboardResponse(
            role,
            DateTime.UtcNow,
            overview,
            statusFunnel,
            actionItems,
            myClasses,
            lowAttendanceStudents,
            pendingVerificationEtrIds,
            myEtrs)
        {
            SystemStats = systemStats,
            MonthlyTrend = monthlyTrend,
            ExpiringStudentsCount = expiringStudentsCount,
            LockedRecords = lockedRecords,
            RecentLockedEtrs = recentLockedEtrs,
            RecentAuditLogs = recentAuditLogs,
            RecentExportJobs = recentExportJobs,
            EvidenceSummary = evidenceSummary,
            ReviewedToday = reviewedToday,
            RecentEvidenceFiles = recentEvidenceFiles,
            TodaySessions = todaySessions,
            PendingSignoffs = pendingSignoffs,
            Profile = profile,
            CertificateSummary = certificateSummary
        };
    }

    private const int MonthlyTrendMonthCount = 8;
    private const int ExpiringSoonDaysThreshold = 30;
    private const int RecentItemsCount = 5;

    private async Task<List<InstructorClassSummary>> ComputeInstructorClassesAsync(int instructorAccountId, CancellationToken cancellationToken)
    {
        var classIds = _unitOfWork.ClassSubjectRepository.GetQueryable()
            .Where(cs => cs.InstructorAccountId == instructorAccountId)
            .Select(cs => cs.ClassId)
            .Distinct()
            .ToList();
            
        var classes = (await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken))
            .Where(c => classIds.Contains(c.ClassId))
            .ToList();

        var enrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken)).ToList();
        var sessions = (await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken)).ToList();
        var etrs = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken)).ToList();
        var subjectResults = (await _unitOfWork.SubjectResultRepository.GetAllAsync(cancellationToken)).ToList();

        return classes
            .Select(c =>
            {
                var classEnrollmentIds = enrollments.Where(e => e.ClassId == c.ClassId).Select(e => e.EnrollmentId).ToHashSet();
                var classEtrIds = etrs.Where(e => classEnrollmentIds.Contains(e.EnrollmentId)).Select(e => e.ETRCourseRecordId).ToHashSet();
                var attendanceRates = subjectResults
                    .Where(sr => classEtrIds.Contains(sr.EtrId) && sr.AttendanceRate.HasValue)
                    .Select(sr => sr.AttendanceRate!.Value)
                    .ToList();

                return new InstructorClassSummary(
                    c.ClassId,
                    c.ClassCode,
                    c.ClassName,
                    classEnrollmentIds.Count,
                    attendanceRates.Count > 0 ? Math.Round(attendanceRates.Average(), 2) : 0,
                    sessions.Count(s => s.ClassId == c.ClassId));
            })
            .ToList();
    }

    private async Task<List<SessionSummary>> ComputeTodaySessionsAsync(HashSet<int> classIds, CancellationToken cancellationToken)
    {
        var classes = (await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken)).ToDictionary(c => c.ClassId, c => c);
        var today = DateTime.UtcNow.Date;

        return (await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken))
            .Where(s => classIds.Contains(s.ClassId) && s.SessionDate.HasValue && s.SessionDate.Value.Date == today)
            .Select(s => new SessionSummary(
                s.SessionId,
                s.SessionTitle,
                s.ClassId,
                classes.GetValueOrDefault(s.ClassId)?.ClassCode ?? "-",
                s.SessionDate,
                s.IsConfirmed))
            .ToList();
    }

    // "Pending signoff" = SubjectResults belonging to a subject the instructor is assigned to teach
    // that have already been evaluated (a Status was recorded) but have no SubjectSignoff yet — the
    // same "missing signoff" check EtrService's completion-progress uses, scoped to this instructor.
    private async Task<int> ComputePendingSignoffsCountAsync(int instructorAccountId, CancellationToken cancellationToken)
    {
        var assignedSubjectIds = _unitOfWork.ClassSubjectRepository.GetQueryable()
            .Where(cs => cs.InstructorAccountId == instructorAccountId)
            .Select(cs => new { cs.ClassId, cs.SubjectId })
            .ToList();

        var enrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken)).ToList();
        var etrs = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken)).ToList();
        var signoffs = (await _unitOfWork.SubjectSignoffRepository.GetAllAsync(cancellationToken)).ToList();

        var assignedClassIds = assignedSubjectIds.Select(a => a.ClassId).ToHashSet();
        var myEnrollmentIds = enrollments.Where(e => assignedClassIds.Contains(e.ClassId)).Select(e => e.EnrollmentId).ToHashSet();
        var myEtrIds = etrs.Where(e => myEnrollmentIds.Contains(e.EnrollmentId)).Select(e => e.ETRCourseRecordId).ToHashSet();

        var subjectResults = (await _unitOfWork.SubjectResultRepository.GetAllAsync(cancellationToken))
            .Where(sr => myEtrIds.Contains(sr.EtrId)
                && assignedSubjectIds.Any(a => a.SubjectId == sr.SubjectId))
            .ToList();

        return subjectResults.Count(sr => !signoffs.Any(s => s.SubjectResultId == sr.SubjectResultId));
    }

    private async Task<List<StudentEtrSummary>> ComputeMyEtrsAsync(int studentAccountId, CancellationToken cancellationToken)
    {
        var myEnrollmentIds = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken))
            .Where(e => e.AccountId == studentAccountId)
            .Select(e => e.EnrollmentId)
            .ToHashSet();

        var myEtrRecords = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken))
            .Where(e => myEnrollmentIds.Contains(e.EnrollmentId))
            .ToList();

        var result = new List<StudentEtrSummary>();
        foreach (var etr in myEtrRecords)
        {
            var progress = await _etrService.GetCompletionProgressAsync(etr.ETRCourseRecordId, cancellationToken);
            result.Add(new StudentEtrSummary(etr.ETRCourseRecordId, etr.Status, progress.PercentComplete, etr.ExpiryDate));
        }

        return result;
    }

    private async Task<List<RecentLockedEtrSummary>> ComputeRecentLockedEtrsAsync(CancellationToken cancellationToken)
    {
        var etrs = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken))
            .Where(e => e.IsLocked)
            .OrderByDescending(e => e.CompletedAt ?? e.CreatedAt)
            .Take(RecentItemsCount)
            .ToList();
        if (etrs.Count == 0) return [];

        var enrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken)).ToList();
        var classes = (await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken)).ToList();
        var courses = (await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken)).ToDictionary(c => c.CourseId, c => c);
        var profiles = (await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken)).ToList();
        var approvalRequests = (await _unitOfWork.ApprovalRequestRepository.GetAllAsync(cancellationToken)).ToList();
        var approvalHistory = (await _unitOfWork.ApprovalHistoryRepository.GetAllAsync(cancellationToken)).ToList();

        var result = new List<RecentLockedEtrSummary>();
        foreach (var etr in etrs)
        {
            var enrollment = enrollments.FirstOrDefault(e => e.EnrollmentId == etr.EnrollmentId);
            var trainingClass = enrollment == null ? null : classes.FirstOrDefault(c => c.ClassId == enrollment.ClassId);
            var course = trainingClass == null ? null : courses.GetValueOrDefault(trainingClass.CourseId);
            var learner = enrollment == null ? null : profiles.FirstOrDefault(p => p.AccountId == enrollment.AccountId);

            var approvalRequest = approvalRequests.FirstOrDefault(a => a.ETRCourseRecordId == etr.ETRCourseRecordId);
            var approver = approvalRequest == null
                ? null
                : approvalHistory
                    .Where(h => h.ApprovalRequestId == approvalRequest.ApprovalRequestId
                        && string.Equals(h.ActionType, "Approve", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(h => h.ActionAt)
                    .FirstOrDefault();
            var approvedByName = approver == null ? null : profiles.FirstOrDefault(p => p.AccountId == approver.ActionByAccountId)?.FullName;

            result.Add(new RecentLockedEtrSummary(
                etr.ETRCourseRecordId,
                learner?.FullName ?? "-",
                course?.CourseName ?? "-",
                approvedByName,
                etr.CompletedAt));
        }

        return result;
    }

    private async Task<List<AuditLogResponse>> ComputeRecentAuditLogsAsync(CancellationToken cancellationToken)
    {
        return (await _unitOfWork.AuditLogRepository.GetAllAsync(cancellationToken))
            .OrderByDescending(l => l.AuditLogId)
            .Take(RecentItemsCount)
            .Select(l => new AuditLogResponse(
                l.AuditLogId, l.AccountId, l.ETRRecordId, l.ActionType, l.EntityName, l.RecordId,
                l.OldValue, l.NewValue, l.Description, l.IPAddress, l.UserAgent, l.CreatedAt))
            .ToList();
    }

    private async Task<List<ExportJobResponse>> ComputeRecentExportJobsAsync(CancellationToken cancellationToken)
    {
        return (await _unitOfWork.ExportJobRepository.GetAllAsync(cancellationToken))
            .OrderByDescending(j => j.RequestedAt)
            .Take(RecentItemsCount)
            .Select(j => new ExportJobResponse(
                j.ExportJobId, j.RequestedByAccountId, j.ExportType, j.FileName ?? "-", j.FilePath ?? "-",
                j.Status, j.RequestedAt, j.CompletedAt, j.DownloadExpiredAt, j.ETRCourseRecordId))
            .ToList();
    }

    private async Task<int> ComputeReviewedTodayCountAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        return (await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken))
            .Count(e => e.VerifiedAt.HasValue && e.VerifiedAt.Value.Date == today);
    }

    private async Task<List<RecentEvidenceFileSummary>> ComputeRecentEvidenceFilesAsync(CancellationToken cancellationToken)
    {
        var evidenceFiles = (await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken))
            .OrderByDescending(e => e.UploadedAt)
            .Take(RecentItemsCount)
            .ToList();
        if (evidenceFiles.Count == 0) return [];

        var profiles = (await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken)).ToList();
        var attachments = (await _unitOfWork.AttachmentRepository.GetAllAsync(cancellationToken))
            .Where(a => a.OwnerType == nameof(EvidenceFile) && evidenceFiles.Select(e => e.EvidenceFileId).Contains(a.OwnerId))
            .ToDictionary(a => a.OwnerId);

        return evidenceFiles
            .Select(e => new RecentEvidenceFileSummary(
                e.EvidenceFileId,
                attachments.GetValueOrDefault(e.EvidenceFileId)?.FileName ?? string.Empty,
                profiles.FirstOrDefault(p => p.AccountId == e.AccountId)?.FullName ?? "-",
                e.VerificationStatus,
                e.UploadedAt))
            .ToList();
    }

    private async Task<StudentProfileSummary?> ComputeStudentProfileAsync(int accountId, CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId, cancellationToken);
        var profile = (await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(p => p.AccountId == accountId);
        if (account == null || profile == null) return null;

        return new StudentProfileSummary(profile.FullName, account.Username, profile.UserCode);
    }

    // "Certificate" in this system is a Completed+ExpiryDate-bearing ETRCourseRecord rather than a
    // separate entity — reuses the Expired/ExpiringSoon threshold already computed for MyEtrs.
    private static CertificateSummary ComputeCertificateSummary(IEnumerable<StudentEtrSummary> myEtrs)
    {
        var completed = myEtrs.Where(e => e.Status == EtrStatus.Completed).ToList();
        var now = DateTime.UtcNow;

        var valid = completed.Count(e => !e.ExpiryDate.HasValue || e.ExpiryDate.Value >= now.AddDays(ExpiringSoonDaysThreshold));
        var expiringSoon = completed.Count(e => e.ExpiryDate.HasValue && e.ExpiryDate.Value >= now && e.ExpiryDate.Value < now.AddDays(ExpiringSoonDaysThreshold));
        var expired = completed.Count(e => e.ExpiryDate.HasValue && e.ExpiryDate.Value < now);

        var recent = completed
            .OrderByDescending(e => e.ExpiryDate ?? DateTime.MinValue)
            .Take(RecentItemsCount)
            .ToList();

        return new CertificateSummary(completed.Count, valid, expiringSoon, expired, recent);
    }
}
