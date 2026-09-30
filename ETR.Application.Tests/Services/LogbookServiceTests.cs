using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;
using Xunit;

namespace ETR.Application.Tests.Services;

public class LogbookServiceTests
{
    [Fact]
    public async Task GetStudentLogbookSummaryAsync_ComputesCorrectTotals_SeparatingFlightAndSimHours()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();

        var studentProfile = new UserProfile
        {
            AccountId = 10,
            UserCode = "STU-010",
            FullName = "Nguyen Pilot",
            Email = "pilot@etr.com"
        };
        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { studentProfile });
        uow.Setup(u => u.UserProfileRepository).Returns(profileRepo.Object);

        var enrollments = new List<CourseEnrollment>
        {
            new() { EnrollmentId = 1, AccountId = 10, ClassId = 100, IsDeleted = false }
        };
        var enrRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);
        uow.Setup(u => u.CourseEnrollmentRepository).Returns(enrRepo.Object);

        var sessions = new List<Session>
        {
            new() { SessionId = 1, ClassId = 100, SubjectId = 1, SessionTitle = "Flight Ex 1", TrainingType = TrainingType.Flight, SessionDate = new DateTime(2026, 10, 5), IsConfirmed = true },
            new() { SessionId = 2, ClassId = 100, SubjectId = 1, SessionTitle = "Flight Ex 2", TrainingType = TrainingType.Flight, SessionDate = new DateTime(2026, 10, 6), IsConfirmed = true },
            new() { SessionId = 3, ClassId = 100, SubjectId = 2, SessionTitle = "SIM Session 1", TrainingType = TrainingType.Simulator, SessionDate = new DateTime(2026, 10, 7), IsConfirmed = true }
        };
        var sessionRepo = new Mock<IGenericRepository<Session>>();
        sessionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);
        uow.Setup(u => u.SessionRepository).Returns(sessionRepo.Object);

        var records = new List<AttendanceRecord>
        {
            // Flight Session 1: 2.0h Flight, 2.0h Dual, 1.0h Night, 1.0h Instrument, 2 Day landings, 1 Night landing
            new()
            {
                AttendanceRecordId = 101,
                SessionId = 1,
                EnrollmentId = 1,
                Status = AttendanceStatus.Present,
                FlightHours = 2.0m,
                SimulatorHours = null,
                DualHours = 2.0m,
                SoloHours = 0m,
                PicHours = 0m,
                NightHours = 1.0m,
                InstrumentHours = 1.0m,
                CrossCountryHours = 0m,
                DayLandings = 2,
                NightLandings = 1,
                InstructorSignedAt = DateTime.UtcNow,
                InstructorSignedByAccountId = 2,
                IsDeleted = false
            },
            // Flight Session 2: 1.5h Flight, 1.5h Solo, 1.5h PIC, 1.5h CrossCountry, 2 Day landings
            new()
            {
                AttendanceRecordId = 102,
                SessionId = 2,
                EnrollmentId = 1,
                Status = AttendanceStatus.Present,
                FlightHours = 1.5m,
                SimulatorHours = null,
                DualHours = 0m,
                SoloHours = 1.5m,
                PicHours = 1.5m,
                NightHours = 0m,
                InstrumentHours = 0m,
                CrossCountryHours = 1.5m,
                DayLandings = 2,
                NightLandings = 0,
                InstructorSignedAt = DateTime.UtcNow,
                InstructorSignedByAccountId = 2,
                IsDeleted = false
            },
            // Simulator Session 3: 4.0h Simulator, 4.0h Dual, 2.0h Instrument
            new()
            {
                AttendanceRecordId = 103,
                SessionId = 3,
                EnrollmentId = 1,
                Status = AttendanceStatus.Present,
                FlightHours = null,
                SimulatorHours = 4.0m,
                DualHours = 4.0m,
                SoloHours = 0m,
                PicHours = 0m,
                NightHours = 0m,
                InstrumentHours = 2.0m,
                CrossCountryHours = 0m,
                DayLandings = 0,
                NightLandings = 0,
                InstructorSignedAt = DateTime.UtcNow,
                InstructorSignedByAccountId = 2,
                IsDeleted = false
            }
        };

        var attRepo = new Mock<IGenericRepository<AttendanceRecord>>();
        attRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);
        uow.Setup(u => u.AttendanceRecordRepository).Returns(attRepo.Object);

        var service = new LogbookService(uow.Object, currentUserService.Object);

        // Act
        var result = await service.GetStudentLogbookSummaryAsync(10, currentAccountId: 10, roleName: "Student");

        // Assert
        // Total Flight Hours = 2.0 + 1.5 = 3.5h (SIM 4.0h is NEVER added to Flight)
        Assert.Equal(3.5m, result.TotalFlightHours);
        // Total Simulator Hours = 4.0h
        Assert.Equal(4.0m, result.TotalSimulatorHours);
        // Dual = 2.0 (flight) + 4.0 (sim) = 6.0m
        Assert.Equal(6.0m, result.TotalDualHours);
        // Solo = 1.5m
        Assert.Equal(1.5m, result.TotalSoloHours);
        // PIC = 1.5m
        Assert.Equal(1.5m, result.TotalPicHours);
        // Night = 1.0m
        Assert.Equal(1.0m, result.TotalNightHours);
        // Instrument = 1.0 + 2.0 = 3.0m
        Assert.Equal(3.0m, result.TotalInstrumentHours);
        // CrossCountry = 1.5m
        Assert.Equal(1.5m, result.TotalCrossCountryHours);
        // Landings: 4 Day + 1 Night = 5 Total
        Assert.Equal(4, result.TotalDayLandings);
        Assert.Equal(1, result.TotalNightLandings);
        Assert.Equal(5, result.TotalLandings);

        Assert.Equal(3, result.TotalSignedSessionsCount);
        Assert.Equal(new DateTime(2026, 10, 6), result.LastFlightDate);
        Assert.Equal(3, result.Entries.Count);
    }

    [Fact]
    public async Task GetStudentLogbookSummaryAsync_ExcludesSoftDeletedUnsignedAndAbsentRecords()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();

        var studentProfile = new UserProfile { AccountId = 10, UserCode = "STU-010", FullName = "Nguyen Pilot" };
        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { studentProfile });
        uow.Setup(u => u.UserProfileRepository).Returns(profileRepo.Object);

        var enrollments = new List<CourseEnrollment>
        {
            new() { EnrollmentId = 1, AccountId = 10, ClassId = 100, IsDeleted = false }
        };
        var enrRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(enrollments);
        uow.Setup(u => u.CourseEnrollmentRepository).Returns(enrRepo.Object);

        var sessions = new List<Session>
        {
            new() { SessionId = 1, ClassId = 100, TrainingType = TrainingType.Flight, IsConfirmed = true },
            new() { SessionId = 2, ClassId = 100, TrainingType = TrainingType.Flight, IsConfirmed = false }, // Not confirmed session
            new() { SessionId = 3, ClassId = 100, TrainingType = TrainingType.Flight, IsConfirmed = true }
        };
        var sessionRepo = new Mock<IGenericRepository<Session>>();
        sessionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sessions);
        uow.Setup(u => u.SessionRepository).Returns(sessionRepo.Object);

        var records = new List<AttendanceRecord>
        {
            // Valid signed record: 2.0h Flight
            new()
            {
                AttendanceRecordId = 101, SessionId = 1, EnrollmentId = 1,
                Status = AttendanceStatus.Present, FlightHours = 2.0m,
                InstructorSignedAt = DateTime.UtcNow, InstructorSignedByAccountId = 2,
                IsDeleted = false
            },
            // Soft-deleted record: 3.0h Flight (Should be ignored)
            new()
            {
                AttendanceRecordId = 102, SessionId = 1, EnrollmentId = 1,
                Status = AttendanceStatus.Present, FlightHours = 3.0m,
                InstructorSignedAt = DateTime.UtcNow, InstructorSignedByAccountId = 2,
                IsDeleted = true
            },
            // Unsigned record on unconfirmed session: 2.5h Flight (Should be ignored)
            new()
            {
                AttendanceRecordId = 103, SessionId = 2, EnrollmentId = 1,
                Status = AttendanceStatus.Present, FlightHours = 2.5m,
                InstructorSignedAt = null, InstructorSignedByAccountId = null,
                IsDeleted = false
            },
            // Absent record: 1.0h Flight (Should be ignored)
            new()
            {
                AttendanceRecordId = 104, SessionId = 3, EnrollmentId = 1,
                Status = AttendanceStatus.Absent, FlightHours = 1.0m,
                InstructorSignedAt = DateTime.UtcNow, InstructorSignedByAccountId = 2,
                IsDeleted = false
            }
        };

        var attRepo = new Mock<IGenericRepository<AttendanceRecord>>();
        attRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(records);
        uow.Setup(u => u.AttendanceRecordRepository).Returns(attRepo.Object);

        var service = new LogbookService(uow.Object, currentUserService.Object);

        // Act
        var result = await service.GetStudentLogbookSummaryAsync(10, currentAccountId: 10, roleName: "Student");

        // Assert: Only the first valid record is counted
        Assert.Equal(2.0m, result.TotalFlightHours);
        Assert.Equal(1, result.TotalSignedSessionsCount);
        Assert.Single(result.Entries);
    }

    [Fact]
    public async Task GetStudentLogbookSummaryAsync_StudentViewingOtherStudent_ThrowsUnauthorizedAccessException()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        var service = new LogbookService(uow.Object, currentUserService.Object);

        // Student 20 tries to access logbook of Student 10
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetStudentLogbookSummaryAsync(10, currentAccountId: 20, roleName: "Student"));
    }

    [Fact]
    public async Task GetStudentLogbookSummaryAsync_LegacyProfileNullRecords_ReturnsZeroSummary()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();

        var studentProfile = new UserProfile
        {
            AccountId = 99,
            UserCode = "STU-099",
            FullName = "Legacy Ground Student"
        };
        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { studentProfile });
        uow.Setup(u => u.UserProfileRepository).Returns(profileRepo.Object);

        var enrRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CourseEnrollment>());
        uow.Setup(u => u.CourseEnrollmentRepository).Returns(enrRepo.Object);

        var sessionRepo = new Mock<IGenericRepository<Session>>();
        sessionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Session>());
        uow.Setup(u => u.SessionRepository).Returns(sessionRepo.Object);

        var attRepo = new Mock<IGenericRepository<AttendanceRecord>>();
        attRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AttendanceRecord>());
        uow.Setup(u => u.AttendanceRecordRepository).Returns(attRepo.Object);

        var service = new LogbookService(uow.Object, currentUserService.Object);

        // Act
        var result = await service.GetStudentLogbookSummaryAsync(99, currentAccountId: 1, roleName: "Admin");

        // Assert
        Assert.Equal(0m, result.TotalFlightHours);
        Assert.Equal(0m, result.TotalSimulatorHours);
        Assert.Equal(0, result.TotalSignedSessionsCount);
        Assert.Null(result.LastFlightDate);
        Assert.Empty(result.Entries);
    }
}
