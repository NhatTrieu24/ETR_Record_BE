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
            .ReturnsAsync(new Course { CourseId = 1, CourseCode = "CRS-01" });
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
            .ReturnsAsync(new Course { CourseId = 1, CourseCode = "CRS-01" });
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
}
