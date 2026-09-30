using ClosedXML.Excel;
using ETR.Application.Compliance;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;
using Xunit;

namespace ETR.Application.Tests.Services;

public class ImportServiceClassRosterTests
{
    private static MemoryStream CreateClassRosterWorkbook(string classCode, string username)
    {
        var workbook = new XLWorkbook();
        var wsClasses = workbook.Worksheets.Add("Classes");
        wsClasses.Cell(1, 1).Value = "Header";
        wsClasses.Cell(2, 1).Value = "Mã lớp*";

        var wsStudents = workbook.Worksheets.Add("Students");
        wsStudents.Cell(1, 1).Value = "Header";
        wsStudents.Cell(2, 1).Value = "Mã lớp*";
        wsStudents.Cell(2, 2).Value = "Username (email)*";

        wsStudents.Cell(3, 1).Value = classCode;
        wsStudents.Cell(3, 2).Value = username;

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    [Theory]
    [InlineData(ClassStatus.InProgress, 7)]
    [InlineData(ClassStatus.Completed, 7)]
    [InlineData(ClassStatus.Cancelled, 7)]
    [InlineData(ClassStatus.Planned, -3)]
    public async Task ValidateClassRosterImportAsync_ShouldRejectStudents_WhenTargetExistingClassIsInProgressCompletedCancelledOrPastStartDate(ClassStatus status, int daysFromNow)
    {
        var mockUow = new Mock<IUnitOfWork>();
        var mockClsSvc = new Mock<IClassService>();
        var mockEnrSvc = new Mock<IEnrollmentService>();

        var existingClass = new Class
        {
            ClassId = 10,
            ClassCode = "B737-C1",
            CourseId = 1,
            StartDate = DateTime.UtcNow.AddDays(daysFromNow),
            Status = status,
            IsDeleted = false
        };

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Class> { existingClass });

        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Course> { new() { CourseId = 1, CourseCode = "B737", Status = CourseStatus.Active } });

        var accRepo = new Mock<IGenericRepository<Account>>();
        accRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { new() { AccountId = 100, Username = "student1@etr.com", RoleId = 6 } });

        var roleRepo = new Mock<IGenericRepository<Role>>();
        roleRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role> { new() { RoleId = 6, RoleName = "Student" } });

        var profRepo = new Mock<IGenericRepository<UserProfile>>();
        profRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { new() { AccountId = 100, FullName = "Student One" } });

        var enrRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseEnrollment>());

        var etrRepo = new Mock<IETRCourseRecordRepository>();
        etrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ETRCourseRecord>());

        var csRepo = new Mock<IGenericRepository<CourseSubject>>();
        csRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = 1, SubjectId = 10 } });

        var subjRepo = new Mock<IGenericRepository<Subject>>();
        subjRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject> { new() { SubjectId = 10, SubjectCode = "GS-101", SubjectType = "Ground" } });

        mockUow.Setup(u => u.ClassRepository).Returns(classRepo.Object);
        mockUow.Setup(u => u.CourseRepository).Returns(courseRepo.Object);
        mockUow.Setup(u => u.AccountRepository).Returns(accRepo.Object);
        mockUow.Setup(u => u.RoleRepository).Returns(roleRepo.Object);
        mockUow.Setup(u => u.UserProfileRepository).Returns(profRepo.Object);
        mockUow.Setup(u => u.CourseEnrollmentRepository).Returns(enrRepo.Object);
        mockUow.Setup(u => u.ETRCourseRecordRepository).Returns(etrRepo.Object);
        mockUow.Setup(u => u.CourseSubjectRepository).Returns(csRepo.Object);
        mockUow.Setup(u => u.SubjectRepository).Returns(subjRepo.Object);

        var service = new ImportService(mockUow.Object, mockClsSvc.Object, mockEnrSvc.Object);

        using var stream = CreateClassRosterWorkbook("B737-C1", "student1@etr.com");
        var result = await service.ValidateClassRosterImportAsync(stream);

        Assert.False(result.CanCommit);
        Assert.Contains(result.Errors, e => e.Column == "Students.ClassCode");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public async Task ValidateClassRosterImportAsync_ShouldAcceptStudents_WhenTargetExistingClassIsPlannedAndStartsTodayOrFuture(int daysFromToday)
    {
        var mockUow = new Mock<IUnitOfWork>();
        var mockClsSvc = new Mock<IClassService>();
        var mockEnrSvc = new Mock<IEnrollmentService>();

        var existingClass = new Class
        {
            ClassId = 10,
            ClassCode = "B737-C1",
            CourseId = 1,
            StartDate = AcademyTimeHelper.GetToday().AddDays(daysFromToday),
            Status = ClassStatus.Planned,
            IsDeleted = false
        };

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Class> { existingClass });

        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Course> { new() { CourseId = 1, CourseCode = "B737", Status = CourseStatus.Active } });

        var accRepo = new Mock<IGenericRepository<Account>>();
        accRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { new() { AccountId = 100, Username = "student1@etr.com", RoleId = 6 } });

        var roleRepo = new Mock<IGenericRepository<Role>>();
        roleRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role> { new() { RoleId = 6, RoleName = "Student" } });

        var profRepo = new Mock<IGenericRepository<UserProfile>>();
        profRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { new() { AccountId = 100, FullName = "Student One" } });

        var enrRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseEnrollment>());

        var etrRepo = new Mock<IETRCourseRecordRepository>();
        etrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ETRCourseRecord>());

        var csRepo = new Mock<IGenericRepository<CourseSubject>>();
        csRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = 1, SubjectId = 10 } });

        var subjRepo = new Mock<IGenericRepository<Subject>>();
        subjRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject> { new() { SubjectId = 10, SubjectCode = "GS-101", SubjectType = "Ground" } });

        mockUow.Setup(u => u.ClassRepository).Returns(classRepo.Object);
        mockUow.Setup(u => u.CourseRepository).Returns(courseRepo.Object);
        mockUow.Setup(u => u.AccountRepository).Returns(accRepo.Object);
        mockUow.Setup(u => u.RoleRepository).Returns(roleRepo.Object);
        mockUow.Setup(u => u.UserProfileRepository).Returns(profRepo.Object);
        mockUow.Setup(u => u.CourseEnrollmentRepository).Returns(enrRepo.Object);
        mockUow.Setup(u => u.ETRCourseRecordRepository).Returns(etrRepo.Object);
        mockUow.Setup(u => u.CourseSubjectRepository).Returns(csRepo.Object);
        mockUow.Setup(u => u.SubjectRepository).Returns(subjRepo.Object);

        var service = new ImportService(mockUow.Object, mockClsSvc.Object, mockEnrSvc.Object);

        using var stream = CreateClassRosterWorkbook("B737-C1", "student1@etr.com");
        var result = await service.ValidateClassRosterImportAsync(stream);

        Assert.True(result.CanCommit);
        Assert.Empty(result.Errors);
    }
}
