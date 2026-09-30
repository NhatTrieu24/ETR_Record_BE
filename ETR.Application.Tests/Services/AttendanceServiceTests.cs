using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;
using Xunit;

namespace ETR.Application.Tests.Services;

public class AttendanceServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IGenericRepository<AttendanceRecord>> _mockAttendanceRepo;
    private readonly Mock<IGenericRepository<Session>> _mockSessionRepo;
    private readonly Mock<IGenericRepository<CourseEnrollment>> _mockEnrollmentRepo;
    private readonly Mock<IGenericRepository<Class>> _mockClassRepo;
    private readonly Mock<IGenericRepository<ClassSubject>> _mockClassSubjectRepo;
    private readonly Mock<IETRCourseRecordRepository> _mockEtrRepo;
    private readonly Mock<IGenericRepository<SubjectResult>> _mockSubjectResultRepo;
    private readonly Mock<IAuditLogRepository> _mockAuditRepo;
    private readonly AttendanceService _service;

    public AttendanceServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockAttendanceRepo = new Mock<IGenericRepository<AttendanceRecord>>();
        _mockSessionRepo = new Mock<IGenericRepository<Session>>();
        _mockEnrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        _mockClassRepo = new Mock<IGenericRepository<Class>>();
        _mockClassSubjectRepo = new Mock<IGenericRepository<ClassSubject>>();
        _mockEtrRepo = new Mock<IETRCourseRecordRepository>();
        _mockSubjectResultRepo = new Mock<IGenericRepository<SubjectResult>>();
        _mockAuditRepo = new Mock<IAuditLogRepository>();

        _mockUow.Setup(u => u.AttendanceRecordRepository).Returns(_mockAttendanceRepo.Object);
        _mockUow.Setup(u => u.SessionRepository).Returns(_mockSessionRepo.Object);
        _mockUow.Setup(u => u.CourseEnrollmentRepository).Returns(_mockEnrollmentRepo.Object);
        _mockUow.Setup(u => u.ClassRepository).Returns(_mockClassRepo.Object);
        _mockUow.Setup(u => u.ClassSubjectRepository).Returns(_mockClassSubjectRepo.Object);
        _mockUow.Setup(u => u.ETRCourseRecordRepository).Returns(_mockEtrRepo.Object);
        _mockUow.Setup(u => u.SubjectResultRepository).Returns(_mockSubjectResultRepo.Object);
        _mockUow.Setup(u => u.AuditLogRepository).Returns(_mockAuditRepo.Object);

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<AttendanceRecordResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<AttendanceRecordResponse>>, CancellationToken>((op, ct) => op(ct));
        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<AttendanceSessionResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<AttendanceSessionResponse>>, CancellationToken>((op, ct) => op(ct));
        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<bool>>, CancellationToken>((op, ct) => op(ct));

        _service = new AttendanceService(_mockUow.Object);
    }

    [Fact]
    public async Task RecordAttendanceAsync_TheorySession_WithFlightHours_ShouldThrow()
    {
        var session = new Session { SessionId = 1, ClassId = 10, SubjectId = 100, TrainingType = TrainingType.Theory, IsConfirmed = false };
        _mockSessionRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var request = new CreateAttendanceRecordRequest(
            SessionId: 1,
            EnrollmentId: 5,
            Status: AttendanceStatus.Present,
            FlightHours: 1.5m
        );

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.RecordAttendanceAsync(request, 99, "Admin"));
        Assert.Contains("Theory sessions cannot record flight or simulator hours", ex.Message);
    }

    [Fact]
    public async Task RecordAttendanceAsync_TheorySession_WithAircraftRegistration_ShouldThrow()
    {
        var session = new Session { SessionId = 1, ClassId = 10, SubjectId = 100, TrainingType = TrainingType.Theory, IsConfirmed = false };
        _mockSessionRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var request = new CreateAttendanceRecordRequest(
            SessionId: 1,
            EnrollmentId: 5,
            Status: AttendanceStatus.Present,
            AircraftRegistration: "VN-C172"
        );

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.RecordAttendanceAsync(request, 99, "Admin"));
        Assert.Contains("Theory sessions cannot record aircraft registration", ex.Message);
    }

    [Fact]
    public async Task RecordAttendanceAsync_FlightSession_WithSimulatorHours_ShouldThrow()
    {
        var session = new Session { SessionId = 2, ClassId = 10, SubjectId = 100, TrainingType = TrainingType.Flight, IsConfirmed = false };
        _mockSessionRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var request = new CreateAttendanceRecordRequest(
            SessionId: 2,
            EnrollmentId: 5,
            Status: AttendanceStatus.Present,
            SimulatorHours: 2.0m
        );

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.RecordAttendanceAsync(request, 99, "Admin"));
        Assert.Contains("Flight sessions cannot record simulator hours", ex.Message);
    }

    [Fact]
    public async Task RecordAttendanceAsync_SimulatorSession_WithRealFlightHours_ShouldThrow()
    {
        var session = new Session { SessionId = 3, ClassId = 10, SubjectId = 100, TrainingType = TrainingType.Simulator, IsConfirmed = false };
        _mockSessionRepo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var request = new CreateAttendanceRecordRequest(
            SessionId: 3,
            EnrollmentId: 5,
            Status: AttendanceStatus.Present,
            FlightHours: 2.5m
        );

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.RecordAttendanceAsync(request, 99, "Admin"));
        Assert.Contains("Simulator sessions cannot record real flight hours", ex.Message);
    }

    [Fact]
    public async Task RecordAttendanceAsync_AbsentStudent_WithHoursOrSatisfactoryGrade_ShouldThrow()
    {
        var session = new Session { SessionId = 4, ClassId = 10, SubjectId = 100, TrainingType = TrainingType.Flight, IsConfirmed = false };
        _mockSessionRepo.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var requestWithHours = new CreateAttendanceRecordRequest(
            SessionId: 4,
            EnrollmentId: 5,
            Status: AttendanceStatus.Absent,
            FlightHours: 1.0m
        );

        var ex1 = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.RecordAttendanceAsync(requestWithHours, 99, "Admin"));
        Assert.Contains("Absent student cannot record training hours", ex1.Message);

        var requestWithSatisfactory = new CreateAttendanceRecordRequest(
            SessionId: 4,
            EnrollmentId: 5,
            Status: AttendanceStatus.Absent,
            PerformanceGrade: PerformanceGrade.Satisfactory
        );

        var ex2 = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.RecordAttendanceAsync(requestWithSatisfactory, 99, "Admin"));
        Assert.Contains("Absent student cannot receive a Satisfactory grade", ex2.Message);
    }

    [Fact]
    public async Task RecordAttendanceAsync_FlightSession_ValidPayload_ShouldSucceed()
    {
        int sessionId = 10;
        int classId = 20;
        int subjectId = 30;
        int enrollmentId = 40;
        int instructorId = 50;

        var session = new Session
        {
            SessionId = sessionId,
            ClassId = classId,
            SubjectId = subjectId,
            TrainingType = TrainingType.Flight,
            LessonCode = "NAV-L01",
            IsConfirmed = false,
            SessionDate = DateTime.UtcNow.Date
        };
        var trainingClass = new Class { ClassId = classId };
        var enrollment = new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = classId, AccountId = 100 };
        var classSubject = new ClassSubject { ClassId = classId, SubjectId = subjectId, InstructorAccountId = instructorId };

        _mockSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>())).ReturnsAsync(trainingClass);
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);
        _mockAttendanceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AttendanceRecord>());
        _mockClassSubjectRepo.Setup(r => r.GetQueryable()).Returns(new List<ClassSubject> { classSubject }.AsQueryable());
        _mockEtrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ETRCourseRecord>());

        var request = new CreateAttendanceRecordRequest(
            SessionId: sessionId,
            EnrollmentId: enrollmentId,
            Status: AttendanceStatus.Present,
            Remarks: "Good circuit maneuvers",
            PerformanceGrade: PerformanceGrade.Satisfactory,
            FlightHours: 1.8m,
            DualHours: 1.8m,
            DayLandings: 4,
            NightLandings: 0,
            AircraftRegistration: "VN-A689",
            DepartureIcao: "VVTS",
            ArrivalIcao: "VVTS",
            Route: "Circuit Pattern",
            InstructorComments: "Smooth touchdown"
        );

        AttendanceRecord? captured = null;
        _mockAttendanceRepo.Setup(r => r.AddAsync(It.IsAny<AttendanceRecord>(), It.IsAny<CancellationToken>()))
            .Callback<AttendanceRecord, CancellationToken>((r, _) => captured = r)
            .Returns(Task.CompletedTask);

        var result = await _service.RecordAttendanceAsync(request, instructorId, "Instructor");

        Assert.NotNull(result);
        Assert.Equal(AttendanceStatus.Present, result.Status);
        Assert.Equal(PerformanceGrade.Satisfactory, result.PerformanceGrade);
        Assert.Equal(1.8m, result.FlightHours);
        Assert.Equal(1.8m, result.DualHours);
        Assert.Equal(4, result.DayLandings);
        Assert.Equal("VN-A689", result.AircraftRegistration);
        Assert.Equal("VVTS", result.DepartureIcao);
        Assert.Equal(TrainingType.Flight, result.SessionTrainingType);
        Assert.Equal("NAV-L01", result.SessionLessonCode);
    }

    [Fact]
    public async Task InstructorSignOffAsync_ValidAssignedInstructor_ShouldSign()
    {
        int recordId = 1;
        int sessionId = 10;
        int classId = 20;
        int subjectId = 30;
        int instructorId = 50;

        var record = new AttendanceRecord
        {
            AttendanceRecordId = recordId,
            SessionId = sessionId,
            EnrollmentId = 100,
            Status = AttendanceStatus.Present,
            PerformanceGrade = PerformanceGrade.Satisfactory,
            FlightHours = 2.0m
        };
        var session = new Session { SessionId = sessionId, ClassId = classId, SubjectId = subjectId, TrainingType = TrainingType.Flight };
        var trainingClass = new Class { ClassId = classId };
        var classSubject = new ClassSubject { ClassId = classId, SubjectId = subjectId, InstructorAccountId = instructorId };

        _mockAttendanceRepo.Setup(r => r.GetByIdAsync(recordId, It.IsAny<CancellationToken>())).ReturnsAsync(record);
        _mockSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>())).ReturnsAsync(trainingClass);
        _mockClassSubjectRepo.Setup(r => r.GetQueryable()).Returns(new List<ClassSubject> { classSubject }.AsQueryable());

        var signRequest = new SignAttendanceRecordRequest("Approved maneuvers");
        var result = await _service.InstructorSignOffAsync(recordId, signRequest, instructorId, "Instructor");

        Assert.NotNull(result);
        Assert.NotNull(result.InstructorSignedAt);
        Assert.Equal(instructorId, result.InstructorSignedByAccountId);
        Assert.Equal("Approved maneuvers", result.InstructorComments);
    }

    [Fact]
    public async Task StudentSignOffAsync_DifferentStudent_ShouldThrowForbidden()
    {
        int recordId = 1;
        int enrollmentId = 100;
        int studentAccountId = 77;
        int anotherStudentAccountId = 88;

        var record = new AttendanceRecord
        {
            AttendanceRecordId = recordId,
            SessionId = 10,
            EnrollmentId = enrollmentId
        };
        var enrollment = new CourseEnrollment
        {
            EnrollmentId = enrollmentId,
            AccountId = studentAccountId
        };

        _mockAttendanceRepo.Setup(r => r.GetByIdAsync(recordId, It.IsAny<CancellationToken>())).ReturnsAsync(record);
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);

        var signRequest = new SignAttendanceRecordRequest("Confirmed");
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _service.StudentSignOffAsync(recordId, signRequest, anotherStudentAccountId, "Student"));
    }

    [Fact]
    public async Task StudentSignOffAsync_ValidStudent_ShouldSign()
    {
        int recordId = 1;
        int enrollmentId = 100;
        int studentAccountId = 77;

        var record = new AttendanceRecord
        {
            AttendanceRecordId = recordId,
            SessionId = 10,
            EnrollmentId = enrollmentId
        };
        var enrollment = new CourseEnrollment
        {
            EnrollmentId = enrollmentId,
            AccountId = studentAccountId
        };
        var session = new Session { SessionId = 10, TrainingType = TrainingType.Flight };

        _mockAttendanceRepo.Setup(r => r.GetByIdAsync(recordId, It.IsAny<CancellationToken>())).ReturnsAsync(record);
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);
        _mockSessionRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var signRequest = new SignAttendanceRecordRequest("Acknowledged feedback");
        var result = await _service.StudentSignOffAsync(recordId, signRequest, studentAccountId, "Student");

        Assert.NotNull(result);
        Assert.NotNull(result.StudentSignedAt);
        Assert.Equal(studentAccountId, result.StudentSignedByAccountId);
        Assert.Equal("Acknowledged feedback", result.StudentComments);
    }

    [Fact]
    public async Task UpdateAttendanceRecordAsync_WhenSigned_ShouldInvalidateSignatures()
    {
        int recordId = 1;
        int sessionId = 10;
        int classId = 20;
        int subjectId = 30;
        int instructorId = 50;

        var record = new AttendanceRecord
        {
            AttendanceRecordId = recordId,
            SessionId = sessionId,
            EnrollmentId = 100,
            Status = AttendanceStatus.Present,
            FlightHours = 1.0m,
            InstructorSignedAt = DateTime.UtcNow.AddHours(-1),
            InstructorSignedByAccountId = instructorId,
            StudentSignedAt = DateTime.UtcNow.AddMinutes(-30),
            StudentSignedByAccountId = 77
        };
        var session = new Session { SessionId = sessionId, ClassId = classId, SubjectId = subjectId, TrainingType = TrainingType.Flight, IsConfirmed = false };
        var trainingClass = new Class { ClassId = classId };
        var classSubject = new ClassSubject { ClassId = classId, SubjectId = subjectId, InstructorAccountId = instructorId };

        _mockAttendanceRepo.Setup(r => r.GetByIdAsync(recordId, It.IsAny<CancellationToken>())).ReturnsAsync(record);
        _mockSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>())).ReturnsAsync(trainingClass);
        _mockClassSubjectRepo.Setup(r => r.GetQueryable()).Returns(new List<ClassSubject> { classSubject }.AsQueryable());
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(new CourseEnrollment { EnrollmentId = 100, ClassId = classId });
        _mockEtrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ETRCourseRecord>());

        var updateRequest = new UpdateAttendanceRecordRequest(
            Status: AttendanceStatus.Present,
            FlightHours: 1.5m,
            Remarks: "Updated flight time"
        );

        var result = await _service.UpdateAttendanceRecordAsync(recordId, updateRequest, instructorId, "Instructor");

        Assert.NotNull(result);
        Assert.Null(result.InstructorSignedAt);
        Assert.Null(result.InstructorSignedByAccountId);
        Assert.Null(result.StudentSignedAt);
        Assert.Null(result.StudentSignedByAccountId);
        Assert.Equal(1.5m, result.FlightHours);
    }

    [Fact]
    public async Task ConfirmSessionAsync_FlightSession_UnsignedRecords_ShouldThrow()
    {
        int sessionId = 10;
        int classId = 20;
        int subjectId = 30;

        var session = new Session
        {
            SessionId = sessionId,
            ClassId = classId,
            SubjectId = subjectId,
            TrainingType = TrainingType.Flight,
            IsConfirmed = false
        };

        var records = new List<AttendanceRecord>
        {
            new() { AttendanceRecordId = 1, SessionId = sessionId, EnrollmentId = 101, Status = AttendanceStatus.Present, InstructorSignedAt = null },
            new() { AttendanceRecordId = 2, SessionId = sessionId, EnrollmentId = 102, Status = AttendanceStatus.Present, InstructorSignedAt = DateTime.UtcNow }
        };

        _mockSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _mockAttendanceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(records);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.ConfirmSessionAsync(sessionId, 99));

        Assert.Contains("have not been digitally signed by the instructor", ex.Message);
    }

    [Fact]
    public async Task ConfirmSessionAsync_FlightSession_AllRecordsSigned_ShouldSucceed()
    {
        int sessionId = 10;
        int classId = 20;
        int subjectId = 30;

        var session = new Session
        {
            SessionId = sessionId,
            ClassId = classId,
            SubjectId = subjectId,
            TrainingType = TrainingType.Flight,
            IsConfirmed = false
        };

        var records = new List<AttendanceRecord>
        {
            new() { AttendanceRecordId = 1, SessionId = sessionId, EnrollmentId = 101, Status = AttendanceStatus.Present, InstructorSignedAt = DateTime.UtcNow },
            new() { AttendanceRecordId = 2, SessionId = sessionId, EnrollmentId = 102, Status = AttendanceStatus.Present, InstructorSignedAt = DateTime.UtcNow }
        };

        _mockSessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _mockAttendanceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(records);
        _mockEnrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CourseEnrollment>());
        _mockEtrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ETRCourseRecord>());
        _mockSubjectResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SubjectResult>());

        var result = await _service.ConfirmSessionAsync(sessionId, 99);

        Assert.NotNull(result);
        Assert.True(result.IsConfirmed);
    }

    [Fact]
    public async Task AdminStudentSignOverrideAsync_NonAdmin_ShouldThrowForbidden()
    {
        var overrideRequest = new AdminSignOverrideRequest("Student is hospitalized and requested proxy sign");
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _service.AdminStudentSignOverrideAsync(1, overrideRequest, 50, "Instructor"));
    }

    [Fact]
    public async Task AdminStudentSignOverrideAsync_ShortReason_ShouldThrow()
    {
        var overrideRequest = new AdminSignOverrideRequest("Too short");
        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.AdminStudentSignOverrideAsync(1, overrideRequest, 99, "Admin"));
        Assert.Contains("at least 10 characters", ex.Message);
    }

    [Fact]
    public async Task AdminStudentSignOverrideAsync_ValidAdmin_ShouldSignWithAudit()
    {
        int recordId = 1;
        int enrollmentId = 100;
        int studentAccountId = 77;
        int adminAccountId = 99;

        var record = new AttendanceRecord
        {
            AttendanceRecordId = recordId,
            SessionId = 10,
            EnrollmentId = enrollmentId,
            StudentComments = "Initial notes"
        };
        var enrollment = new CourseEnrollment
        {
            EnrollmentId = enrollmentId,
            AccountId = studentAccountId
        };
        var session = new Session { SessionId = 10, TrainingType = TrainingType.Flight };

        _mockAttendanceRepo.Setup(r => r.GetByIdAsync(recordId, It.IsAny<CancellationToken>())).ReturnsAsync(record);
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);
        _mockSessionRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var overrideRequest = new AdminSignOverrideRequest("Student authorized admin sign-off due to emergency leave");
        var result = await _service.AdminStudentSignOverrideAsync(recordId, overrideRequest, adminAccountId, "Admin");

        Assert.NotNull(result);
        Assert.NotNull(result.StudentSignedAt);
        Assert.Equal(adminAccountId, result.StudentSignedByAccountId);
        Assert.Contains("[Admin Override by Account #99: Student authorized admin sign-off due to emergency leave]", result.StudentComments);
    }

    [Theory]
    [InlineData(1.0, 0, 0, 0, 0, 0, null, null, "Simulator sessions cannot record real flight hours")]
    [InlineData(0, 1.0, 0, 0, 0, 0, null, null, "Simulator sessions cannot record solo flight hours")]
    [InlineData(0, 0, 1.0, 0, 0, 0, null, null, "Simulator sessions cannot record PIC flight hours")]
    [InlineData(0, 0, 0, 1.0, 0, 0, null, null, "Simulator sessions cannot record real night flight hours")]
    [InlineData(0, 0, 0, 0, 1.0, 0, null, null, "Simulator sessions cannot record cross-country flight hours")]
    [InlineData(0, 0, 0, 0, 0, 2, null, null, "Simulator sessions cannot record real aircraft landings")]
    [InlineData(0, 0, 0, 0, 0, 0, "VN-C172", null, "Simulator sessions cannot record aircraft registration")]
    [InlineData(0, 0, 0, 0, 0, 0, null, "VVTS", "Simulator sessions cannot record real flight route details")]
    public async Task RecordAttendanceAsync_SimulatorInvalidCombinations_ShouldThrow(
        decimal flightHours, decimal soloHours, decimal picHours, decimal nightHours,
        decimal crossCountryHours, int dayLandings, string? aircraftRegistration, string? departureIcao, string expectedErrorSubstr)
    {
        var session = new Session { SessionId = 5, ClassId = 10, SubjectId = 100, TrainingType = TrainingType.Simulator, IsConfirmed = false };
        _mockSessionRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var request = new CreateAttendanceRecordRequest(
            SessionId: 5,
            EnrollmentId: 5,
            Status: AttendanceStatus.Present,
            FlightHours: flightHours > 0 ? flightHours : null,
            SoloHours: soloHours > 0 ? soloHours : null,
            PicHours: picHours > 0 ? picHours : null,
            NightHours: nightHours > 0 ? nightHours : null,
            CrossCountryHours: crossCountryHours > 0 ? crossCountryHours : null,
            DayLandings: dayLandings > 0 ? dayLandings : null,
            AircraftRegistration: aircraftRegistration,
            DepartureIcao: departureIcao
        );

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.RecordAttendanceAsync(request, 99, "Admin"));

        Assert.Contains(expectedErrorSubstr, ex.Message);
    }
}
