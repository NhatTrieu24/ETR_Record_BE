using ETR.Application.Services;

namespace ETR.Application.DTOs;

// Role-aware "my dashboard" — every field is nullable and only the subset relevant to the
// CALLER's role is populated; the rest stay null rather than the FE having to know in advance
// which fields apply to which role.
public record MyDashboardResponse(
    string Role,
    DateTime GeneratedAt,
    DashboardKpis? Overview,
    DashboardStatusFunnel? StatusFunnel,
    DashboardActionItems? ActionItems,
    IEnumerable<InstructorClassSummary>? MyClasses,
    IEnumerable<LowAttendanceStudentResponse>? LowAttendanceStudents,
    IEnumerable<int>? PendingVerificationEtrIds,
    IEnumerable<StudentEtrSummary>? MyEtrs);

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
    int StudentCount);

public record StudentEtrSummary(
    int ETRCourseRecordId,
    string Status,
    decimal PercentComplete,
    DateTime? ExpiryDate);
