using ETR.Application.Services;
using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

// Role-aware "my dashboard" — every field is nullable and only the subset relevant to the
// CALLER's role is populated; the rest stay null rather than the FE having to know in advance
// which fields apply to which role. Extra per-role widgets (SystemStats, MonthlyTrend, ...) are
// declared as init-only properties below rather than positional params so the original
// constructor call site keeps working unchanged.
public record MyDashboardResponse(
    string Role,
    DateTime GeneratedAt,
    DashboardKpis? Overview,
    DashboardStatusFunnel? StatusFunnel,
    DashboardActionItems? ActionItems,
    IEnumerable<InstructorClassSummary>? MyClasses,
    IEnumerable<LowAttendanceStudentResponse>? LowAttendanceStudents,
    IEnumerable<int>? PendingVerificationEtrIds,
    IEnumerable<StudentEtrSummary>? MyEtrs)
{
    // Admin
    public SystemStatsSummary? SystemStats { get; init; }

    // TrainingManager, Auditor
    public MonthlyTrendSummary? MonthlyTrend { get; init; }

    // Academic
    public int? ExpiringStudentsCount { get; init; }

    // Auditor
    public LockedRecordsSummary? LockedRecords { get; init; }
    public IEnumerable<RecentLockedEtrSummary>? RecentLockedEtrs { get; init; }
    public IEnumerable<AuditLogResponse>? RecentAuditLogs { get; init; }
    public IEnumerable<ExportJobResponse>? RecentExportJobs { get; init; }

    // QA
    public EvidenceSummary? EvidenceSummary { get; init; }
    public int? ReviewedToday { get; init; }
    public IEnumerable<RecentEvidenceFileSummary>? RecentEvidenceFiles { get; init; }

    // Instructor
    public IEnumerable<SessionSummary>? TodaySessions { get; init; }
    public int? PendingSignoffs { get; init; }

    // Student
    public StudentProfileSummary? Profile { get; init; }
    public CertificateSummary? CertificateSummary { get; init; }
}

public record DashboardStatusFunnel(
    int Draft,
    int InProgress,
    int Submitted,
    int Verified,
    int Completed,
    int ReturnedForCorrection,
    int Cancelled);

public record InstructorClassSummary(
    int ClassId,
    string ClassCode,
    string ClassName,
    int StudentCount,
    decimal AttendanceRate,
    int SessionCount);

public record StudentEtrSummary(
    int ETRCourseRecordId,
    EtrStatus Status,
    decimal PercentComplete,
    DateTime? ExpiryDate);

public record SystemStatsSummary(
    int TotalUsers,
    int TotalLearners,
    int TotalInstructors,
    int TotalCourses,
    int TotalClasses,
    int ActiveAccounts,
    int NewUsersThisMonth);

public record MonthlyTrendSummary(
    IReadOnlyList<string> Months,
    IReadOnlyList<int> Locked,
    IReadOnlyList<int> Returned);

public record LockedRecordsSummary(
    int TotalLocked,
    decimal ComplianceRate);

public record RecentLockedEtrSummary(
    int ETRCourseRecordId,
    string LearnerName,
    string CourseName,
    string? ApprovedBy,
    DateTime? CompletedAt);

public record EvidenceSummary(
    int Total,
    int Verified,
    int Pending,
    int Rejected);

public record RecentEvidenceFileSummary(
    int EvidenceFileId,
    string FileName,
    string LearnerName,
    string VerificationStatus,
    DateTime UploadedAt);

public record SessionSummary(
    int SessionId,
    string SessionTitle,
    int ClassId,
    string ClassCode,
    DateTime? SessionDate,
    bool IsConfirmed);

public record StudentProfileSummary(
    string FullName,
    string Username,
    string UserCode);

public record CertificateSummary(
    int Total,
    int Valid,
    int ExpiringSoon,
    int Expired,
    IEnumerable<StudentEtrSummary> Recent);
