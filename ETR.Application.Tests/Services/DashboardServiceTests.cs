using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using Moq;

namespace ETR.Application.Tests.Services;

public class DashboardServiceTests
{
    private static (DashboardService Service, Mock<IUnitOfWork> UnitOfWork, Mock<IAttendanceService> AttendanceService, Mock<IEtrService> EtrService)
        BuildService(string? roleName, int? accountId)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.RoleName).Returns(roleName);
        currentUser.SetupGet(c => c.AccountId).Returns(accountId);

        var etrRepo = new Mock<IETRCourseRecordRepository>();
        etrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ETRCourseRecord>());
        unitOfWork.SetupGet(u => u.ETRCourseRecordRepository).Returns(etrRepo.Object);

        var approvalRepo = new Mock<IGenericRepository<ApprovalRequest>>();
        approvalRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApprovalRequest>());
        unitOfWork.SetupGet(u => u.ApprovalRequestRepository).Returns(approvalRepo.Object);

        var evidenceRepo = new Mock<IGenericRepository<EvidenceFile>>();
        evidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<EvidenceFile>());
        unitOfWork.SetupGet(u => u.EvidenceFileRepository).Returns(evidenceRepo.Object);

        var subjectResultRepo = new Mock<IGenericRepository<SubjectResult>>();
        subjectResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SubjectResult>());
        unitOfWork.SetupGet(u => u.SubjectResultRepository).Returns(subjectResultRepo.Object);

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Class>());
        unitOfWork.SetupGet(u => u.ClassRepository).Returns(classRepo.Object);

        var enrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CourseEnrollment>());
        unitOfWork.SetupGet(u => u.CourseEnrollmentRepository).Returns(enrollmentRepo.Object);

        var attendanceService = new Mock<IAttendanceService>();
        attendanceService.Setup(a => a.GetLowAttendanceStudentsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<LowAttendanceStudentResponse>());

        var etrService = new Mock<IEtrService>();

        var service = new DashboardService(unitOfWork.Object, currentUser.Object, attendanceService.Object, etrService.Object);
        return (service, unitOfWork, attendanceService, etrService);
    }

    [Fact]
    public async Task GetMyDashboardAsync_ForAdmin_PopulatesOverviewStatusFunnelAndActionItems_OnlyAdmin()
    {
        var (service, _, _, _) = BuildService("Admin", accountId: 1);

        var result = await service.GetMyDashboardAsync(CancellationToken.None);

        Assert.Equal("Admin", result.Role);
        Assert.NotNull(result.Overview);
        Assert.NotNull(result.StatusFunnel);
        Assert.NotNull(result.ActionItems);
        Assert.Null(result.MyClasses);
        Assert.Null(result.LowAttendanceStudents);
        Assert.Null(result.PendingVerificationEtrIds);
        Assert.Null(result.MyEtrs);
    }

    [Fact]
    public async Task GetMyDashboardAsync_ForInstructor_PopulatesMyClassesWithCorrectStudentCount()
    {
        var (service, unitOfWork, _, _) = BuildService("Instructor", accountId: 42);

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Class>
        {
            new() { ClassId = 1, ClassCode = "C1", ClassName = "Class One", InstructorAccountId = 42 },
            new() { ClassId = 2, ClassCode = "C2", ClassName = "Class Two", InstructorAccountId = 99 }
        });
        unitOfWork.SetupGet(u => u.ClassRepository).Returns(classRepo.Object);

        var enrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CourseEnrollment>
        {
            new() { EnrollmentId = 1, ClassId = 1, AccountId = 100 },
            new() { EnrollmentId = 2, ClassId = 1, AccountId = 101 },
            new() { EnrollmentId = 3, ClassId = 2, AccountId = 102 }
        });
        unitOfWork.SetupGet(u => u.CourseEnrollmentRepository).Returns(enrollmentRepo.Object);

        var result = await service.GetMyDashboardAsync(CancellationToken.None);

        Assert.Equal("Instructor", result.Role);
        Assert.NotNull(result.MyClasses);
        var myClass = Assert.Single(result.MyClasses!);
        Assert.Equal(1, myClass.ClassId);
        Assert.Equal(2, myClass.StudentCount);
        Assert.Null(result.Overview);
        Assert.Null(result.MyEtrs);
    }

    [Fact]
    public async Task GetMyDashboardAsync_ForQA_PopulatesOnlySubmittedEtrIds()
    {
        var (service, unitOfWork, _, _) = BuildService("QA", accountId: 3);

        var etrRepo = new Mock<IETRCourseRecordRepository>();
        etrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 1, Status = "Submitted" },
            new() { ETRCourseRecordId = 2, Status = "InProgress" },
            new() { ETRCourseRecordId = 3, Status = "Submitted" }
        });
        unitOfWork.SetupGet(u => u.ETRCourseRecordRepository).Returns(etrRepo.Object);

        var result = await service.GetMyDashboardAsync(CancellationToken.None);

        Assert.NotNull(result.PendingVerificationEtrIds);
        Assert.Equal(new[] { 1, 3 }, result.PendingVerificationEtrIds!.OrderBy(x => x));
        Assert.Null(result.MyClasses);
    }

    [Fact]
    public async Task GetMyDashboardAsync_ForStudent_PopulatesOnlyOwnEtrsWithCompletionProgress()
    {
        var (service, unitOfWork, _, etrService) = BuildService("Student", accountId: 7);

        var enrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CourseEnrollment>
        {
            new() { EnrollmentId = 1, AccountId = 7, ClassId = 1 },
            new() { EnrollmentId = 2, AccountId = 999, ClassId = 1 }
        });
        unitOfWork.SetupGet(u => u.CourseEnrollmentRepository).Returns(enrollmentRepo.Object);

        var etrRepo = new Mock<IETRCourseRecordRepository>();
        etrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 10, EnrollmentId = 1, Status = "InProgress" },
            new() { ETRCourseRecordId = 20, EnrollmentId = 2, Status = "InProgress" }
        });
        unitOfWork.SetupGet(u => u.ETRCourseRecordRepository).Returns(etrRepo.Object);

        etrService.Setup(e => e.GetCompletionProgressAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EtrCompletionProgressResponse(10, 4, 2, 50m, Enumerable.Empty<CompletionCheckItem>()));

        var result = await service.GetMyDashboardAsync(CancellationToken.None);

        Assert.NotNull(result.MyEtrs);
        var myEtr = Assert.Single(result.MyEtrs!);
        Assert.Equal(10, myEtr.ETRCourseRecordId);
        Assert.Equal(50m, myEtr.PercentComplete);
        Assert.Null(result.Overview);
    }
}
