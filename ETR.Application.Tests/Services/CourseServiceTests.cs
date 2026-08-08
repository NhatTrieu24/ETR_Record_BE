using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using Moq;

namespace ETR.Application.Tests.Services;

public class CourseServiceTests
{
    private static (CourseService Service, Mock<IUnitOfWork> UnitOfWork, Course Course, Mock<IAuditLogRepository> AuditLogRepo) BuildService(Course course)
    {
        var unitOfWork = new Mock<IUnitOfWork>();

        unitOfWork.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<CourseResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<CourseResponse>> op, CancellationToken ct) => op(ct));
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetByIdAsync(course.CourseId, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        unitOfWork.SetupGet(u => u.CourseRepository).Returns(courseRepo.Object);

        var courseSubjectRepo = new Mock<IGenericRepository<CourseSubject>>();
        courseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = course.CourseId, SubjectId = 1, SequenceNo = 1 } });
        unitOfWork.SetupGet(u => u.CourseSubjectRepository).Returns(courseSubjectRepo.Object);

        var subjectRepo = new Mock<IGenericRepository<Subject>>();
        subjectRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Subject { SubjectId = 1, SubjectName = "Safety" });
        unitOfWork.SetupGet(u => u.SubjectRepository).Returns(subjectRepo.Object);

        var auditLogRepo = new Mock<IAuditLogRepository>();
        unitOfWork.SetupGet(u => u.AuditLogRepository).Returns(auditLogRepo.Object);

        return (new CourseService(unitOfWork.Object), unitOfWork, course, auditLogRepo);
    }

    private static UpdateCourseRequest BuildRequest(Course course, int? validityMonths) => new(
        course.CourseId, course.CourseCode, course.CourseName, course.Description, course.DurationHours, course.Status,
        validityMonths, course.CourseType,
        new List<AddCourseSubjectRequest> { new() { SubjectId = 1, SequenceNo = 1 } });

    [Fact]
    public async Task UpdateCourseAsync_ValidityMonthsChanged_BumpsVersionNoAndUpdatesEffectiveFrom()
    {
        var course = new Course { CourseId = 1, CourseCode = "C1", CourseName = "Safety", Status = "Active", ValidityMonths = 12, VersionNo = 1 };
        var (service, _, _, auditLogRepo) = BuildService(course);

        var response = await service.UpdateCourseAsync(1, BuildRequest(course, validityMonths: 24), updatedByAccountId: 9, CancellationToken.None);

        Assert.Equal(2, response.VersionNo);
        Assert.Equal(2, course.VersionNo);
        auditLogRepo.Verify(r => r.AddAsync(It.Is<AuditLog>(a => a.Description!.Contains("VersionNo bumped")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCourseAsync_ValidityMonthsUnchanged_DoesNotBumpVersionNo()
    {
        var course = new Course { CourseId = 1, CourseCode = "C1", CourseName = "Safety", Status = "Active", ValidityMonths = 12, VersionNo = 1 };
        var (service, _, _, _) = BuildService(course);

        var request = BuildRequest(course, validityMonths: 12) with { CourseName = "Safety Training (renamed)" };
        var response = await service.UpdateCourseAsync(1, request, updatedByAccountId: 9, CancellationToken.None);

        Assert.Equal(1, response.VersionNo);
        Assert.Equal("Safety Training (renamed)", response.CourseName);
    }
}
