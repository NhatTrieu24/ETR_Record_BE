using ClosedXML.Excel;
using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;
using Xunit;

namespace ETR.Application.Tests.Services;

public class ClassServiceTests
{
    [Fact]
    public void ClassDurationValidator_CalculatesCorrectMinDuration_ForGroundAndSim()
    {
        var startDate = new DateTime(2026, 10, 1);
        var subjects = new List<(int RequiredHours, string? SubjectType)>
        {
            (16, "Theory"),     // 16 / 8 = 2 days
            (8, "Practical"),   // 8 / 4 = 2 days
        };

        var (minTrainingDays, minBufferDays, totalMinDays, minEndDate) =
            ClassDurationValidator.CalculateMinDuration(startDate, subjects);

        // 2 + 2 = 4 training days.
        // Buffer = Ceiling(4 * 0.15) = Ceiling(0.6) = 1 day.
        // Total = 5 days.
        Assert.Equal(4, minTrainingDays);
        Assert.Equal(1, minBufferDays);
        Assert.Equal(5, totalMinDays);
        Assert.Equal(new DateTime(2026, 10, 6), minEndDate);
    }

    [Fact]
    public async Task CreateClassCoreAsync_ThrowsWhenStartDateIsInPast()
    {
        var uow = new Mock<IUnitOfWork>();
        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = 1, CourseCode = "CRS-01", Status = CourseStatus.Active, VersionNo = 2 });
        uow.Setup(u => u.CourseRepository).Returns(courseRepo.Object);

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Class>());
        uow.Setup(u => u.ClassRepository).Returns(classRepo.Object);

        var currentUserService = new Mock<ICurrentUserService>();
        var service = new ClassService(uow.Object, currentUserService.Object);

        var request = new CreateClassRequest(
            "CLS-01", "Class 1", 1,
            DateTime.UtcNow.AddDays(-2), // In past
            DateTime.UtcNow.AddDays(30),
            "Phòng Sim A320", 30, ClassStatus.Planned);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.CreateClassCoreAsync(request, createdByAccountId: 1));

        Assert.Contains("quá khứ", ex.Message);
    }

    [Fact]
    public async Task CreateClassCoreAsync_ThrowsWhenEndDateIsShorterThanIcaoMinDuration()
    {
        var uow = new Mock<IUnitOfWork>();
        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = 1, CourseCode = "CRS-01", Status = CourseStatus.Active, VersionNo = 1 });
        uow.Setup(u => u.CourseRepository).Returns(courseRepo.Object);

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Class>());
        uow.Setup(u => u.ClassRepository).Returns(classRepo.Object);

        var courseSubRepo = new Mock<IGenericRepository<CourseSubject>>();
        courseSubRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject>
            {
                new() { CourseId = 1, SubjectId = 10, RequiredHours = 40 } // 40 / 8 = 5 days + 1 buffer = 6 days
            });
        uow.Setup(u => u.CourseSubjectRepository).Returns(courseSubRepo.Object);

        var subRepo = new Mock<IGenericRepository<Subject>>();
        subRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject>
            {
                new() { SubjectId = 10, SubjectType = "Theory" }
            });
        uow.Setup(u => u.SubjectRepository).Returns(subRepo.Object);

        var currentUserService = new Mock<ICurrentUserService>();
        var service = new ClassService(uow.Object, currentUserService.Object);

        var start = DateTime.UtcNow.AddDays(1);
        var end = start.AddDays(2); // Only 2 days, but requires 6 days

        var request = new CreateClassRequest(
            "CLS-01", "Class 1", 1, start, end,
            "Phòng Sim A320", 30, ClassStatus.Planned);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.CreateClassCoreAsync(request, createdByAccountId: 1));

        Assert.Contains("ICAO/CAAV", ex.Message);
    }

    [Theory]
    [InlineData(CourseStatus.Draft)]
    [InlineData(CourseStatus.Archived)]
    [InlineData(CourseStatus.Inactive)]
    public async Task CreateClassCoreAsync_ThrowsWhenCourseIsNotActive(CourseStatus nonActiveStatus)
    {
        var uow = new Mock<IUnitOfWork>();
        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = 1, CourseCode = "CRS-01", Status = nonActiveStatus, VersionNo = 1 });
        uow.Setup(u => u.CourseRepository).Returns(courseRepo.Object);

        var currentUserService = new Mock<ICurrentUserService>();
        var service = new ClassService(uow.Object, currentUserService.Object);

        var request = new CreateClassRequest(
            "CLS-01", "Class 1", 1, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(30),
            "Phòng Sim A320", 30, ClassStatus.Planned);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.CreateClassCoreAsync(request, createdByAccountId: 1));

        Assert.Contains("Chỉ có thể mở lớp cho khóa học đang Hoạt động", ex.Message);
    }

    [Fact]
    public async Task GenerateClassRosterImportTemplateAsync_GeneratesAll3Sheets()
    {
        var uow = new Mock<IUnitOfWork>();
        var clsSvc = new Mock<IClassService>();
        var enrSvc = new Mock<IEnrollmentService>();

        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Course> { new() { CourseId = 1, CourseCode = "CRS-01" } });
        uow.Setup(u => u.CourseRepository).Returns(courseRepo.Object);

        var subRepo = new Mock<IGenericRepository<Subject>>();
        subRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject> { new() { SubjectId = 1, SubjectCode = "SJ-01" } });
        uow.Setup(u => u.SubjectRepository).Returns(subRepo.Object);

        var roleRepo = new Mock<IGenericRepository<Role>>();
        roleRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role> { new() { RoleId = 2, RoleName = "Instructor" } });
        uow.Setup(u => u.RoleRepository).Returns(roleRepo.Object);

        var accRepo = new Mock<IGenericRepository<Account>>();
        accRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { new() { AccountId = 5, Username = "ins@etr.com", RoleId = 2 } });
        uow.Setup(u => u.AccountRepository).Returns(accRepo.Object);

        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        var bytes = await service.GenerateClassRosterImportTemplateAsync();
        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);

        Assert.Equal(3, workbook.Worksheets.Count);
        Assert.NotNull(workbook.Worksheet("Classes"));
        Assert.NotNull(workbook.Worksheet("Instructors"));
        Assert.NotNull(workbook.Worksheet("Students"));
    }

    [Fact]
    public async Task CreateClassCoreAsync_GeneratesCorrectLocationTitleAndTrainingType_ForCabinMaintSimAndFlight()
    {
        var uow = new Mock<IUnitOfWork>();
        var addedSessions = new List<Session>();

        var course = new Course
        {
            CourseId = 1,
            CourseCode = "MULTI-01",
            CourseName = "Multi-Discipline Pilot and Tech Training",
            Status = CourseStatus.Active,
            VersionNo = 1
        };

        var subjects = new List<Subject>
        {
            new() { SubjectId = 1, SubjectCode = "CABIN-EMERGENCY", SubjectName = "Cabin Safety Drill", SubjectType = "Practical" },
            new() { SubjectId = 2, SubjectCode = "MAINT-01", SubjectName = "Line Maintenance Workshop", SubjectType = "Practical" },
            new() { SubjectId = 3, SubjectCode = "A320-SIM", SubjectName = "A320 FSTD Session", SubjectType = "Practical" },
            new() { SubjectId = 4, SubjectCode = "A320-FLT", SubjectName = "A320 Aircraft Flight Training", SubjectType = "Practical" }
        };

        var courseSubjects = new List<CourseSubject>
        {
            new() { CourseId = 1, SubjectId = 1, SequenceNo = 1, RequiredSessions = 1, RequiredHours = 8 },
            new() { CourseId = 1, SubjectId = 2, SequenceNo = 2, RequiredSessions = 1, RequiredHours = 8 },
            new() { CourseId = 1, SubjectId = 3, SequenceNo = 3, RequiredSessions = 1, RequiredHours = 4 },
            new() { CourseId = 1, SubjectId = 4, SequenceNo = 4, RequiredSessions = 1, RequiredHours = 4 }
        };

        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        uow.Setup(u => u.CourseRepository).Returns(courseRepo.Object);

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Class>());
        classRepo.Setup(r => r.GetQueryable()).Returns(new List<Class>().AsQueryable());
        uow.Setup(u => u.ClassRepository).Returns(classRepo.Object);

        var courseSubRepo = new Mock<IGenericRepository<CourseSubject>>();
        courseSubRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(courseSubjects);
        uow.Setup(u => u.CourseSubjectRepository).Returns(courseSubRepo.Object);

        var subRepo = new Mock<IGenericRepository<Subject>>();
        subRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(subjects);
        uow.Setup(u => u.SubjectRepository).Returns(subRepo.Object);

        var classSubRepo = new Mock<IGenericRepository<ClassSubject>>();
        uow.Setup(u => u.ClassSubjectRepository).Returns(classSubRepo.Object);

        var sessionRepo = new Mock<IGenericRepository<Session>>();
        sessionRepo.Setup(r => r.AddAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .Callback<Session, CancellationToken>((s, _) => addedSessions.Add(s))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.SessionRepository).Returns(sessionRepo.Object);

        var assessRepo = new Mock<IGenericRepository<Assessment>>();
        assessRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Assessment>());
        uow.Setup(u => u.AssessmentRepository).Returns(assessRepo.Object);

        var checkRepo = new Mock<IGenericRepository<PracticalChecklist>>();
        checkRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklist>());
        uow.Setup(u => u.PracticalChecklistRepository).Returns(checkRepo.Object);

        var auditRepo = new Mock<IAuditLogRepository>();
        uow.Setup(u => u.AuditLogRepository).Returns(auditRepo.Object);

        var currentUserService = new Mock<ICurrentUserService>();
        var service = new ClassService(uow.Object, currentUserService.Object);

        var start = DateTime.UtcNow.Date.AddDays(1);
        var end = start.AddDays(30);
        var request = new CreateClassRequest(
            "CLS-MULTI", "Multi Training Class", 1, start, end,
            null, 20, ClassStatus.Planned);

        // Act
        var result = await service.CreateClassCoreAsync(request, createdByAccountId: 99);

        // Assert
        Assert.Equal(4, addedSessions.Count);

        var cabinSession = addedSessions.First(s => s.SubjectId == 1);
        Assert.Equal(TrainingType.Theory, cabinSession.TrainingType);
        Assert.Contains("Xưởng thực hành / Phòng huấn luyện an toàn", cabinSession.Location);
        Assert.DoesNotContain("SIM Room", cabinSession.Location);
        Assert.Equal("Buổi 1", cabinSession.SessionTitle);

        var maintSession = addedSessions.First(s => s.SubjectId == 2);
        Assert.Equal(TrainingType.Theory, maintSession.TrainingType);
        Assert.Contains("Xưởng thực hành / Phòng huấn luyện an toàn", maintSession.Location);
        Assert.DoesNotContain("SIM Room", maintSession.Location);
        Assert.Equal("Buổi 1", maintSession.SessionTitle);

        var simSession = addedSessions.First(s => s.SubjectId == 3);
        Assert.Equal(TrainingType.Simulator, simSession.TrainingType);
        Assert.Contains("Buồng lái mô phỏng (SIM / FSTD Room)", simSession.Location);
        Assert.Equal("Buổi 1 (Đánh giá thực hành buồng lái mô phỏng)", simSession.SessionTitle);

        var fltSession = addedSessions.First(s => s.SubjectId == 4);
        Assert.Equal(TrainingType.Flight, fltSession.TrainingType);
        Assert.Contains("Sân bay huấn luyện / Khu vực bay (Airfield)", fltSession.Location);
        Assert.Equal("Buổi 1 (Đánh giá thực hành bay)", fltSession.SessionTitle);
    }

    [Fact]
    public async Task CreateClassCoreAsync_NonFstdSubjectWithChecklist_AssignsProcedureChecklistTitle()
    {
        var uow = new Mock<IUnitOfWork>();
        var addedSessions = new List<Session>();

        var course = new Course
        {
            CourseId = 1,
            CourseCode = "CABIN-01",
            CourseName = "Cabin Safety Course",
            Status = CourseStatus.Active,
            VersionNo = 1
        };

        var subjects = new List<Subject>
        {
            new() { SubjectId = 1, SubjectCode = "CABIN-EMERGENCY", SubjectName = "Cabin Safety Drill", SubjectType = "Practical" }
        };

        var courseSubjects = new List<CourseSubject>
        {
            new() { CourseId = 1, SubjectId = 1, SequenceNo = 1, RequiredSessions = 1, RequiredHours = 8 }
        };

        var checklists = new List<PracticalChecklist>
        {
            new() { PracticalChecklistId = 55, CourseId = 1, SubjectId = 1, ItemName = "Emergency Evac Item", IsDeleted = false }
        };

        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        uow.Setup(u => u.CourseRepository).Returns(courseRepo.Object);

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Class>());
        classRepo.Setup(r => r.GetQueryable()).Returns(new List<Class>().AsQueryable());
        uow.Setup(u => u.ClassRepository).Returns(classRepo.Object);

        var courseSubRepo = new Mock<IGenericRepository<CourseSubject>>();
        courseSubRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(courseSubjects);
        uow.Setup(u => u.CourseSubjectRepository).Returns(courseSubRepo.Object);

        var subRepo = new Mock<IGenericRepository<Subject>>();
        subRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(subjects);
        uow.Setup(u => u.SubjectRepository).Returns(subRepo.Object);

        var classSubRepo = new Mock<IGenericRepository<ClassSubject>>();
        uow.Setup(u => u.ClassSubjectRepository).Returns(classSubRepo.Object);

        var sessionRepo = new Mock<IGenericRepository<Session>>();
        sessionRepo.Setup(r => r.AddAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .Callback<Session, CancellationToken>((s, _) => addedSessions.Add(s))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.SessionRepository).Returns(sessionRepo.Object);

        var assessRepo = new Mock<IGenericRepository<Assessment>>();
        assessRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Assessment>());
        uow.Setup(u => u.AssessmentRepository).Returns(assessRepo.Object);

        var checkRepo = new Mock<IGenericRepository<PracticalChecklist>>();
        checkRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(checklists);
        uow.Setup(u => u.PracticalChecklistRepository).Returns(checkRepo.Object);

        var auditRepo = new Mock<IAuditLogRepository>();
        uow.Setup(u => u.AuditLogRepository).Returns(auditRepo.Object);

        var currentUserService = new Mock<ICurrentUserService>();
        var service = new ClassService(uow.Object, currentUserService.Object);

        var start = DateTime.UtcNow.Date.AddDays(1);
        var end = start.AddDays(30);
        var request = new CreateClassRequest(
            "CLS-CABIN", "Cabin Safety Drill Class", 1, start, end,
            null, 20, ClassStatus.Planned);

        // Act
        var result = await service.CreateClassCoreAsync(request, createdByAccountId: 99);

        // Assert
        Assert.Single(addedSessions);
        var session = addedSessions[0];
        Assert.Equal(TrainingType.Theory, session.TrainingType);
        Assert.Equal(55, session.PracticalChecklistId);
        Assert.True(session.IsChecklistRequired);
        Assert.Equal("Buổi 1 (Đánh giá thực hành quy trình)", session.SessionTitle);
        Assert.Contains("Xưởng thực hành / Phòng huấn luyện an toàn", session.Location);
        Assert.DoesNotContain("SIM Room", session.Location);
    }

    [Fact]
    public async Task CreateClassCoreAsync_CompletedStatus_ThrowsBusinessRuleViolationException()
    {
        var uow = new Mock<IUnitOfWork>();
        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = 1, CourseCode = "CRS-01", Status = CourseStatus.Active, VersionNo = 1 });
        uow.Setup(u => u.CourseRepository).Returns(courseRepo.Object);

        var currentUserService = new Mock<ICurrentUserService>();
        var service = new ClassService(uow.Object, currentUserService.Object);

        var request = new CreateClassRequest(
            "CLS-01", "Class 1", 1, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(30),
            "Phòng Sim A320", 30, ClassStatus.Completed);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.CreateClassCoreAsync(request, createdByAccountId: 1));

        Assert.Contains("Không thể tạo mới lớp học với trạng thái 'Completed'", ex.Message);
    }

    [Fact]
    public async Task UpdateClassAsync_ManualCompletedStatus_ThrowsBusinessRuleViolationException()
    {
        var uow = new Mock<IUnitOfWork>();
        var existingClass = new Class
        {
            ClassId = 1,
            ClassCode = "CLS-01",
            ClassName = "Class 1",
            CourseId = 1,
            Status = ClassStatus.InProgress,
            IsDeleted = false
        };

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingClass);
        classRepo.Setup(r => r.GetQueryable()).Returns(new List<Class> { existingClass }.AsQueryable());
        uow.Setup(u => u.ClassRepository).Returns(classRepo.Object);

        uow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<TrainingClassResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<TrainingClassResponse>>, CancellationToken>((op, ct) => op(ct));

        var currentUserService = new Mock<ICurrentUserService>();
        var service = new ClassService(uow.Object, currentUserService.Object);

        var request = new UpdateClassRequest(
            1, "CLS-01", "Class 1", 1, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(30),
            "Phòng Sim A320", 30, ClassStatus.Completed, null);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.UpdateClassAsync(1, request, updatedByAccountId: 1));

        Assert.Contains("Không thể chuyển trạng thái lớp sang 'Completed' thủ công", ex.Message);
    }

    [Fact]
    public async Task UpdateClassAsync_AlreadyCompletedClass_ThrowsBusinessRuleViolationException()
    {
        var uow = new Mock<IUnitOfWork>();
        var existingClass = new Class
        {
            ClassId = 1,
            ClassCode = "CLS-01",
            ClassName = "Class 1",
            CourseId = 1,
            Status = ClassStatus.Completed,
            IsDeleted = false
        };

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingClass);
        classRepo.Setup(r => r.GetQueryable()).Returns(new List<Class> { existingClass }.AsQueryable());
        uow.Setup(u => u.ClassRepository).Returns(classRepo.Object);

        uow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<TrainingClassResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<TrainingClassResponse>>, CancellationToken>((op, ct) => op(ct));

        var currentUserService = new Mock<ICurrentUserService>();
        var service = new ClassService(uow.Object, currentUserService.Object);

        var request = new UpdateClassRequest(
            1, "CLS-01", "Class 1", 1, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(30),
            "Phòng Sim A320", 30, ClassStatus.InProgress, null);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.UpdateClassAsync(1, request, updatedByAccountId: 1));

        Assert.Contains("Không thể thay đổi trạng thái của lớp học đã hoàn thành", ex.Message);
    }
}

