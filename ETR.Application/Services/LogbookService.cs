using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;

namespace ETR.Application.Services;

public class LogbookService : ILogbookService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public LogbookService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    private async Task<HashSet<int>> GetInstructorStudentIdsAsync(int instructorAccountId, CancellationToken cancellationToken)
    {
        var instructorClassIds = _unitOfWork.ClassSubjectRepository.GetQueryable()
            .Where(cs => cs.InstructorAccountId == instructorAccountId)
            .Select(cs => cs.ClassId)
            .ToHashSet();

        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        return enrollments.Where(e => instructorClassIds.Contains(e.ClassId)).Select(e => e.AccountId).ToHashSet();
    }

    public async Task<LogbookSummaryResponse> GetStudentLogbookSummaryAsync(
        int accountId,
        int currentAccountId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // 1. Phân quyền truy cập (Access Control)
        bool isPrivilegedRole = roleName is "Admin" or "Academic" or "TrainingManager" or "QA" or "Audit";
        if (roleName is "Student" or "Learner")
        {
            if (accountId != currentAccountId)
            {
                throw new UnauthorizedAccessException("Học viên chỉ có quyền truy cập sổ bay (logbook) của chính mình.");
            }
        }
        else if (roleName == "Instructor")
        {
            var myStudentIds = await GetInstructorStudentIdsAsync(currentAccountId, cancellationToken);
            if (!myStudentIds.Contains(accountId))
            {
                throw new KeyNotFoundException($"Không tìm thấy dữ liệu học viên (Account #{accountId}) trong các lớp được phân công giảng dạy.");
            }
        }
        else if (!isPrivilegedRole)
        {
            if (accountId != currentAccountId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem sổ bay (logbook) của học viên này.");
            }
        }

        // 2. Lấy thông tin UserProfile của học viên
        var allProfiles = await _unitOfWork.UserProfileRepository.GetAllIncludingDeletedAsync(cancellationToken);
        var studentProfile = allProfiles.FirstOrDefault(p => p.AccountId == accountId)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ người dùng cho Account #{accountId}.");

        // 3. Lấy danh sách CourseEnrollment của học viên (loại bản ghi xóa mềm)
        var allEnrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        var studentEnrollments = allEnrollments.Where(e => e.AccountId == accountId && !e.IsDeleted).ToList();
        var studentEnrollmentIds = studentEnrollments.Select(e => e.EnrollmentId).ToHashSet();

        // 4. Lấy danh sách Sessions và Subjects
        var allSessions = (await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken))
            .Where(s => !s.IsDeleted)
            .ToDictionary(s => s.SessionId);

        // 5. Lấy các AttendanceRecord hợp lệ:
        // - Thuộc enrollment của học viên
        // - Không bị xóa mềm (!IsDeleted)
        // - Trạng thái có mặt (Present hoặc Late)
        // - Đã được giảng viên ký xác nhận (!string.IsNullOrWhiteSpace(InstructorSignature) hoặc session.IsConfirmed)
        var allAttendanceRecords = await _unitOfWork.AttendanceRecordRepository.GetAllAsync(cancellationToken);
        var qualifiedRecords = allAttendanceRecords
            .Where(ar => !ar.IsDeleted &&
                         studentEnrollmentIds.Contains(ar.EnrollmentId) &&
                         ar.Status == AttendanceStatus.Present &&
                         allSessions.ContainsKey(ar.SessionId))
            .Where(ar =>
            {
                var session = allSessions[ar.SessionId];
                bool isSignedByInstructor = ar.InstructorSignedAt.HasValue || ar.InstructorSignedByAccountId.HasValue;
                bool isSessionConfirmed = session.IsConfirmed;
                return isSignedByInstructor || isSessionConfirmed;
            })
            .ToList();

        // 6. Tính toán tổng giờ (Strict Calculation Rules):
        // - TotalFlightHours: Chỉ tính từ FlightHours của các buổi bay thực (KHÔNG cộng SimulatorHours)
        // - TotalSimulatorHours: Chỉ tính từ SimulatorHours của các buổi SIM/FSTD (KHÔNG cộng vào FlightHours)
        // - Các giờ phân loại phụ (Dual, Solo, PIC, Night, Instrument, CrossCountry) là các chiều phân rã độc lập (KHÔNG cộng chồng vào Total)
        decimal totalFlightHours = 0m;
        decimal totalSimHours = 0m;
        decimal totalDual = 0m;
        decimal totalSolo = 0m;
        decimal totalPic = 0m;
        decimal totalNight = 0m;
        decimal totalInstrument = 0m;
        decimal totalCrossCountry = 0m;
        int totalDayLandings = 0;
        int totalNightLandings = 0;
        int totalLandings = 0;

        var entries = new List<LogbookEntryDto>();
        DateTime? lastFlightDate = null;

        foreach (var ar in qualifiedRecords)
        {
            var session = allSessions[ar.SessionId];

            decimal flightH = ar.FlightHours ?? 0m;
            decimal simH = ar.SimulatorHours ?? 0m;
            decimal dualH = ar.DualHours ?? 0m;
            decimal soloH = ar.SoloHours ?? 0m;
            decimal picH = ar.PicHours ?? 0m;
            decimal nightH = ar.NightHours ?? 0m;
            decimal instH = ar.InstrumentHours ?? 0m;
            decimal ccH = ar.CrossCountryHours ?? 0m;

            int dayL = ar.DayLandings ?? 0;
            int nightL = ar.NightLandings ?? 0;
            int totL = (dayL + nightL);

            totalFlightHours += flightH;
            totalSimHours += simH;
            totalDual += dualH;
            totalSolo += soloH;
            totalPic += picH;
            totalNight += nightH;
            totalInstrument += instH;
            totalCrossCountry += ccH;

            totalDayLandings += dayL;
            totalNightLandings += nightL;
            totalLandings += totL;

            if (session.TrainingType == TrainingType.Flight || flightH > 0)
            {
                if (session.SessionDate.HasValue && (!lastFlightDate.HasValue || session.SessionDate.Value > lastFlightDate.Value))
                {
                    lastFlightDate = session.SessionDate.Value;
                }
            }

            entries.Add(new LogbookEntryDto(
                ar.AttendanceRecordId,
                session.SessionId,
                ar.EnrollmentId,
                session.LessonCode,
                session.SessionTitle,
                session.SessionDate,
                session.TrainingType,
                session.Location,
                ar.AircraftRegistration,
                ar.SimulatorDevice,
                ar.DepartureIcao,
                ar.ArrivalIcao,
                ar.Route,
                flightH,
                simH,
                dualH,
                soloH,
                picH,
                nightH,
                instH,
                ccH,
                dayL,
                nightL,
                totL,
                ar.Remarks,
                ar.InstructorComments,
                ar.StudentComments,
                ar.InstructorSignedAt,
                ar.InstructorSignedByAccountId,
                ar.StudentSignedAt,
                ar.StudentSignedByAccountId
            ));
        }

        var sortedEntries = entries
            .OrderByDescending(e => e.SessionDate)
            .ThenByDescending(e => e.SessionId)
            .ToList();

        return new LogbookSummaryResponse(
            studentProfile.AccountId,
            studentProfile.UserCode,
            studentProfile.FullName,
            totalFlightHours,
            totalSimHours,
            totalDual,
            totalSolo,
            totalPic,
            totalNight,
            totalInstrument,
            0m,
            0m,
            totalCrossCountry,
            totalDayLandings,
            totalNightLandings,
            totalLandings,
            qualifiedRecords.Count,
            lastFlightDate,
            sortedEntries
        );
    }
}
