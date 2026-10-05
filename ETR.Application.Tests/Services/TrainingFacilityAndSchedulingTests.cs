using ETR.Application.DTOs.Facility;
using ETR.Application.DTOs.Session;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace ETR.Application.Tests.Services;

public class TrainingFacilityAndSchedulingTests
{
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly Mock<IGenericRepository<Session>> _mockSessionRepo = new();
    private readonly Mock<IGenericRepository<Class>> _mockClassRepo = new();
    private readonly Mock<IGenericRepository<Subject>> _mockSubjectRepo = new();
    private readonly Mock<IGenericRepository<Assessment>> _mockAssessmentRepo = new();
    private readonly Mock<IGenericRepository<PracticalChecklist>> _mockChecklistRepo = new();
    private readonly Mock<IGenericRepository<ClassSubject>> _mockClassSubjectRepo = new();
    private readonly Mock<IGenericRepository<TrainingFacility>> _mockFacilityRepo = new();
    private readonly Mock<IGenericRepository<UserProfile>> _mockUserProfileRepo = new();
    private readonly Mock<IGenericRepository<Account>> _mockAccountRepo = new();
    private readonly Mock<IAuditLogRepository> _mockAuditRepo = new();

    public TrainingFacilityAndSchedulingTests()
    {
        _mockUow.Setup(u => u.SessionRepository).Returns(_mockSessionRepo.Object);
        _mockUow.Setup(u => u.ClassRepository).Returns(_mockClassRepo.Object);
        _mockUow.Setup(u => u.ClassSubjectRepository).Returns(_mockClassSubjectRepo.Object);
        _mockUow.Setup(u => u.SubjectRepository).Returns(_mockSubjectRepo.Object);
        _mockUow.Setup(u => u.AssessmentRepository).Returns(_mockAssessmentRepo.Object);
        _mockUow.Setup(u => u.PracticalChecklistRepository).Returns(_mockChecklistRepo.Object);
        _mockUow.Setup(u => u.TrainingFacilityRepository).Returns(_mockFacilityRepo.Object);
        _mockUow.Setup(u => u.UserProfileRepository).Returns(_mockUserProfileRepo.Object);
        _mockUow.Setup(u => u.AccountRepository).Returns(_mockAccountRepo.Object);
        _mockUow.Setup(u => u.AuditLogRepository).Returns(_mockAuditRepo.Object);
        _mockUow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Theory]
    [InlineData(TrainingType.Simulator, FacilityType.Simulator, true)]
    [InlineData(TrainingType.Simulator, FacilityType.Classroom, false)]
    [InlineData(TrainingType.Simulator, FacilityType.Airfield, false)]
    [InlineData(TrainingType.Flight, FacilityType.Airfield, true)]
    [InlineData(TrainingType.Flight, FacilityType.Simulator, false)]
    [InlineData(TrainingType.Theory, FacilityType.Classroom, true)]
    [InlineData(TrainingType.Theory, FacilityType.Airfield, false)]
    public void FacilityCompatibilityHelper_ValidatesBaseTrainingTypesCorrectly(
        TrainingType trainingType,
        FacilityType facilityType,
        bool expected)
    {
        var isCompatible = FacilityCompatibilityHelper.IsCompatible(facilityType, trainingType, null);
        Assert.Equal(expected, isCompatible);
    }

    [Theory]
    [InlineData("Aircraft Maintenance Practice", FacilityType.Workshop, true)]
    [InlineData("Aircraft Maintenance Practice", FacilityType.Classroom, true)]
    [InlineData("Aircraft Maintenance Practice", FacilityType.Simulator, false)]
    [InlineData("Cabin Crew Safety Evacuation", FacilityType.Workshop, true)]
    [InlineData("Aviation Meteorology", FacilityType.Classroom, true)]
    [InlineData("Aviation Meteorology", FacilityType.Workshop, false)]
    public void FacilityCompatibilityHelper_ValidatesSubjectTypesCorrectly(
        string subjectName,
        FacilityType facilityType,
        bool expected)
    {
        var subject = new Subject { SubjectId = 1, SubjectName = subjectName, SubjectCode = "SUB-01" };
        var isCompatible = FacilityCompatibilityHelper.IsCompatible(facilityType, TrainingType.Theory, subject);
        Assert.Equal(expected, isCompatible);
    }

    [Fact]
    public void FacilityCompatibilityHelper_DetectsTimeOverlapsAccurately()
    {
        var start1 = new DateTime(2026, 10, 10, 8, 0, 0);
        var end1 = new DateTime(2026, 10, 10, 10, 0, 0);

        // Completely overlapping / identical
        Assert.True(FacilityCompatibilityHelper.HasTimeOverlap(start1, end1, start1, end1));

        // Partial overlap: start2 starts inside [start1, end1)
        var start2 = new DateTime(2026, 10, 10, 9, 0, 0);
        var end2 = new DateTime(2026, 10, 10, 11, 0, 0);
        Assert.True(FacilityCompatibilityHelper.HasTimeOverlap(start1, end1, start2, end2));

        // Contained inside: [8:30, 9:30) inside [8:00, 10:00)
        var start3 = new DateTime(2026, 10, 10, 8, 30, 0);
        var end3 = new DateTime(2026, 10, 10, 9, 30, 0);
        Assert.True(FacilityCompatibilityHelper.HasTimeOverlap(start1, end1, start3, end3));

        // Adjacent: [10:00, 12:00) right after [8:00, 10:00) => No overlap
        var startAdjacent = new DateTime(2026, 10, 10, 10, 0, 0);
        var endAdjacent = new DateTime(2026, 10, 10, 12, 0, 0);
        Assert.False(FacilityCompatibilityHelper.HasTimeOverlap(start1, end1, startAdjacent, endAdjacent));

        // Different times on same day
        var startDiff = new DateTime(2026, 10, 10, 13, 0, 0);
        var endDiff = new DateTime(2026, 10, 10, 15, 0, 0);
        Assert.False(FacilityCompatibilityHelper.HasTimeOverlap(start1, end1, startDiff, endDiff));
    }

    [Fact]
    public async Task UpdateSessionAsync_ThrowsValidationException_WhenAssigningIncompatibleFacility()
    {
        var session = new Session
        {
            SessionId = 101,
            ClassId = 1,
            SubjectId = 5,
            TrainingType = TrainingType.Flight,
            SessionDate = new DateTime(2026, 10, 10)
        };

        var simFacility = new TrainingFacility
        {
            FacilityId = 201,
            FacilityName = "Phòng Sim A320",
            FacilityType = FacilityType.Simulator,
            IsActive = true
        };

        _mockSessionRepo.Setup(r => r.GetByIdAsync(101, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _mockClassRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Class { ClassId = 1, CourseId = 10 });
        _mockFacilityRepo.Setup(r => r.GetByIdAsync(201, It.IsAny<CancellationToken>())).ReturnsAsync(simFacility);
        _mockSubjectRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Subject { SubjectId = 5, SubjectName = "Bay thuc hanh" });
        _mockSessionRepo.Setup(r => r.GetQueryable()).Returns(new List<Session>().AsQueryable());

        var service = new SessionService(_mockUow.Object);
        var request = new UpdateSessionRequest
        {
            SessionTitle = "Bay VFR Solo",
            SessionDate = new DateTime(2026, 10, 10),
            StartAt = new DateTime(2026, 10, 10, 8, 0, 0),
            EndAt = new DateTime(2026, 10, 10, 10, 0, 0),
            TrainingType = TrainingType.Flight,
            FacilityId = 201 // Sim facility not allowed for Flight
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateSessionAsync(101, request, 1));

        Assert.Contains("không tương thích với buổi đào tạo", ex.Message);
    }

    [Fact]
    public async Task UpdateSessionAsync_ThrowsValidationException_WhenFacilityCollidesInTimeSlot()
    {
        var existingSession = new Session
        {
            SessionId = 100,
            ClassId = 1,
            FacilityId = 301,
            SessionDate = new DateTime(2026, 10, 10),
            StartAt = new DateTime(2026, 10, 10, 8, 0, 0),
            EndAt = new DateTime(2026, 10, 10, 10, 0, 0)
        };

        var targetSession = new Session
        {
            SessionId = 102,
            ClassId = 2,
            SubjectId = 5,
            TrainingType = TrainingType.Simulator,
            SessionDate = new DateTime(2026, 10, 10)
        };

        var facility = new TrainingFacility
        {
            FacilityId = 301,
            FacilityName = "Phòng Sim A320",
            FacilityType = FacilityType.Simulator,
            IsActive = true
        };

        _mockSessionRepo.Setup(r => r.GetByIdAsync(102, It.IsAny<CancellationToken>())).ReturnsAsync(targetSession);
        _mockClassRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(new Class { ClassId = 2, CourseId = 10 });
        _mockFacilityRepo.Setup(r => r.GetByIdAsync(301, It.IsAny<CancellationToken>())).ReturnsAsync(facility);
        _mockSessionRepo.Setup(r => r.GetQueryable()).Returns(new List<Session> { existingSession }.AsQueryable());

        var service = new SessionService(_mockUow.Object);
        var request = new UpdateSessionRequest
        {
            SessionTitle = "Sim Flight",
            SessionDate = new DateTime(2026, 10, 10),
            StartAt = new DateTime(2026, 10, 10, 9, 0, 0), // Collides with 8:00 - 10:00
            EndAt = new DateTime(2026, 10, 10, 11, 0, 0),
            TrainingType = TrainingType.Simulator,
            FacilityId = 301
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateSessionAsync(102, request, 1));

        Assert.Contains("đã có lịch sử dụng trùng khung giờ", ex.Message);
    }

    [Fact]
    public async Task UpdateSessionAsync_ThrowsValidationException_WhenInstructorCollidesInTimeSlotAcrossClasses()
    {
        var existingSession = new Session
        {
            SessionId = 100,
            ClassId = 1,
            SubjectId = 10,
            SessionDate = new DateTime(2026, 10, 10),
            StartAt = new DateTime(2026, 10, 10, 8, 0, 0),
            EndAt = new DateTime(2026, 10, 10, 10, 0, 0)
        };

        var targetSession = new Session
        {
            SessionId = 102,
            ClassId = 2,
            SubjectId = 5,
            TrainingType = TrainingType.Theory,
            SessionDate = new DateTime(2026, 10, 10)
        };

        var classSubjects = new List<ClassSubject>
        {
            new ClassSubject { ClassId = 1, SubjectId = 10, InstructorAccountId = 99 },
            new ClassSubject { ClassId = 2, SubjectId = 5, InstructorAccountId = 99 }
        };

        _mockSessionRepo.Setup(r => r.GetByIdAsync(102, It.IsAny<CancellationToken>())).ReturnsAsync(targetSession);
        _mockClassRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(new Class { ClassId = 2, CourseId = 10, ClassCode = "CLS-02" });
        _mockClassRepo.Setup(r => r.GetQueryable()).Returns(new List<Class> { new Class { ClassId = 1, ClassCode = "CLS-01" }, new Class { ClassId = 2, ClassCode = "CLS-02" } }.AsQueryable());
        _mockSessionRepo.Setup(r => r.GetQueryable()).Returns(new List<Session> { existingSession, targetSession }.AsQueryable());
        _mockClassSubjectRepo.Setup(r => r.GetQueryable()).Returns(classSubjects.AsQueryable());

        var service = new SessionService(_mockUow.Object);
        var request = new UpdateSessionRequest
        {
            SessionTitle = "Ly thuyet khong luu",
            SessionDate = new DateTime(2026, 10, 10),
            StartAt = new DateTime(2026, 10, 10, 8, 30, 0), // Collides with 8:00 - 10:00
            EndAt = new DateTime(2026, 10, 10, 10, 30, 0),
            TrainingType = TrainingType.Theory
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateSessionAsync(102, request, 1));

        Assert.Contains("Xung đột lịch giảng viên", ex.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_RejectsStandardSession_AndAllowsRemedialSession()
    {
        var cls = new Class { ClassId = 1, CourseId = 10 };
        _mockClassRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cls);
        _mockSessionRepo.Setup(r => r.GetQueryable()).Returns(new List<Session>().AsQueryable());

        var service = new SessionService(_mockUow.Object);

        // 1. Standard session creation attempted manually => throws BusinessRuleViolationException
        var standardRequest = new CreateSessionRequest
        {
            ClassId = 1,
            SubjectId = 5,
            SessionTitle = "Buổi học tiêu chuẩn thủ công",
            SessionDate = DateTime.UtcNow.AddDays(1),
            TrainingType = TrainingType.Theory,
            IsRemedial = false
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.CreateSessionAsync(standardRequest, 1));
        Assert.Contains("Buổi học chính khóa được hệ thống tự động sinh", ex.Message);

        // 2. Remedial session creation attempted manually => succeeds
        var remedialSubject = new Subject { SubjectId = 5, SubjectName = "Thực hành Buồng lái", SubjectCode = "SIM-01" };
        var remedialClassSubject = new ClassSubject { ClassId = 1, SubjectId = 5, InstructorAccountId = 99 };
        var remedialSession = new Session { SessionId = 55, ClassId = 1, SubjectId = 5, IsRemedial = true, SessionTitle = "Buổi phụ đạo thực hành SIM" };

        _mockSubjectRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(remedialSubject);
        _mockClassSubjectRepo.Setup(r => r.GetQueryable()).Returns(new List<ClassSubject> { remedialClassSubject }.AsQueryable());
        _mockClassSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ClassSubject> { remedialClassSubject });
        _mockSessionRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(remedialSession);
        _mockUserProfileRepo.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserProfile>());
        _mockAccountRepo.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Account>());

        var remedialRequest = new CreateSessionRequest
        {
            ClassId = 1,
            SubjectId = 5,
            SessionTitle = "Buổi phụ đạo thực hành SIM",
            SessionDate = DateTime.UtcNow.AddDays(1),
            StartAt = DateTime.UtcNow.AddDays(1).Date.AddHours(14),
            EndAt = DateTime.UtcNow.AddDays(1).Date.AddHours(16),
            TrainingType = TrainingType.Simulator,
            IsRemedial = true
        };

        var result = await service.CreateSessionAsync(remedialRequest, 1);
        Assert.NotNull(result);
        Assert.True(result.IsRemedial);
        _mockSessionRepo.Verify(r => r.AddAsync(It.Is<Session>(s => s.IsRemedial), It.IsAny<CancellationToken>()), Times.Once);
        _mockUow.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteSessionAsync_BlocksCurriculumSession_AndAllowsRemedialSession()
    {
        var regularSession = new Session { SessionId = 10, ClassId = 1, IsRemedial = false };
        var remedialSession = new Session { SessionId = 11, ClassId = 1, IsRemedial = true };

        _mockSessionRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(regularSession);
        _mockSessionRepo.Setup(r => r.GetByIdAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(remedialSession);

        var service = new SessionService(_mockUow.Object);

        // Deleting regular curriculum session => BusinessRuleViolationException
        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.DeleteSessionAsync(10, 1));
        Assert.Contains("Buổi học chính khóa được sinh tự động", ex.Message);

        // Deleting remedial session => Success
        await service.DeleteSessionAsync(11, 1);
        _mockSessionRepo.Verify(r => r.Update(remedialSession), Times.Once);
        _mockUow.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
