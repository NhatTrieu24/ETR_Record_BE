using ETR.Application.DTOs;
using ETR.Application.Interfaces;

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

        switch (role)
        {
            case "Admin":
            case "TrainingManager":
            case "Academic":
            case "ManagementViewer":
            case "Audit":
                overview = await DashboardKpiCalculator.ComputeAsync(_unitOfWork, cancellationToken);
                statusFunnel = await DashboardKpiCalculator.ComputeStatusFunnelAsync(_unitOfWork, cancellationToken);
                actionItems = await DashboardKpiCalculator.ComputeActionItemsAsync(_unitOfWork, cancellationToken);
                break;

            case "Instructor":
                if (accountId.HasValue)
                {
                    myClasses = await ComputeInstructorClassesAsync(accountId.Value, cancellationToken);

                    var allLowAttendance = await _attendanceService.GetLowAttendanceStudentsAsync(null, cancellationToken);
                    var myClassIds = myClasses.Select(c => c.ClassId).ToHashSet();
                    lowAttendanceStudents = allLowAttendance.Where(s => myClassIds.Contains(s.ClassId)).ToList();
                }
                break;

            case "QA":
                var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
                pendingVerificationEtrIds = etrs.Where(e => e.Status == "Submitted").Select(e => e.ETRCourseRecordId).ToList();
                actionItems = await DashboardKpiCalculator.ComputeActionItemsAsync(_unitOfWork, cancellationToken);
                break;

            case "Student":
                if (accountId.HasValue)
                {
                    myEtrs = await ComputeMyEtrsAsync(accountId.Value, cancellationToken);
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
            myEtrs);
    }

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

        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);

        return classes
            .Select(c => new InstructorClassSummary(
                c.ClassId,
                c.ClassCode,
                c.ClassName,
                enrollments.Count(e => e.ClassId == c.ClassId)))
            .ToList();
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
}
