using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using Moq;

namespace ETR.Application.Tests.Services;

public class EnrollmentServiceTests
{
    private static (EnrollmentService Service, Mock<IUnitOfWork> UnitOfWork, UserProfile Profile) BuildService(
        string initialLearnerStatus,
        List<ETRCourseRecord> existingEtrs,
        List<CourseEnrollment> existingEnrollments,
        List<Class> classes)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUserService>();

        unitOfWork.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<CreateEnrollmentResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<CreateEnrollmentResponse>> op, CancellationToken ct) => op(ct));
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var profile = new UserProfile { AccountId = 100, Status = initialLearnerStatus, UserCode = "U100", FullName = "Learner 100" };
        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        unitOfWork.SetupGet(u => u.UserProfileRepository).Returns(profileRepo.Object);

        var trainingClass = classes.First(c => c.ClassId == 10);
        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(trainingClass);
        classRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(classes);
        unitOfWork.SetupGet(u => u.ClassRepository).Returns(classRepo.Object);

        var courseSubjectRepo = new Mock<IGenericRepository<CourseSubject>>();
        courseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = trainingClass.CourseId, SubjectId = 1 } });
        unitOfWork.SetupGet(u => u.CourseSubjectRepository).Returns(courseSubjectRepo.Object);

        var enrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => existingEnrollments);
        enrollmentRepo.Setup(r => r.AddAsync(It.IsAny<CourseEnrollment>(), It.IsAny<CancellationToken>()))
            .Callback<CourseEnrollment, CancellationToken>((e, _) =>
            {
                e.EnrollmentId = existingEnrollments.Count + 1000;
                existingEnrollments.Add(e);
            })
            .Returns(Task.CompletedTask);
        unitOfWork.SetupGet(u => u.CourseEnrollmentRepository).Returns(enrollmentRepo.Object);

        var etrRepo = new Mock<IETRCourseRecordRepository>();
        etrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => existingEtrs);
        etrRepo.Setup(r => r.AddAsync(It.IsAny<ETRCourseRecord>(), It.IsAny<CancellationToken>()))
            .Callback<ETRCourseRecord, CancellationToken>((e, _) =>
            {
                e.ETRCourseRecordId = existingEtrs.Count + 1000;
                existingEtrs.Add(e);
            })
            .Returns(Task.CompletedTask);
        unitOfWork.SetupGet(u => u.ETRCourseRecordRepository).Returns(etrRepo.Object);

        var classStudentRepo = new Mock<IGenericRepository<ClassStudent>>();
        classStudentRepo.Setup(r => r.AddAsync(It.IsAny<ClassStudent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.SetupGet(u => u.ClassStudentRepository).Returns(classStudentRepo.Object);

        var assessmentRepo = new Mock<IGenericRepository<Assessment>>();
        assessmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Assessment>());
        unitOfWork.SetupGet(u => u.AssessmentRepository).Returns(assessmentRepo.Object);

        var checklistRepo = new Mock<IGenericRepository<PracticalChecklist>>();
        checklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklist>());
        unitOfWork.SetupGet(u => u.PracticalChecklistRepository).Returns(checklistRepo.Object);

        var subjectResultRepo = new Mock<IGenericRepository<SubjectResult>>();
        subjectResultRepo.Setup(r => r.AddAsync(It.IsAny<SubjectResult>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.SetupGet(u => u.SubjectResultRepository).Returns(subjectResultRepo.Object);

        var auditLogRepo = new Mock<IAuditLogRepository>();
        unitOfWork.SetupGet(u => u.AuditLogRepository).Returns(auditLogRepo.Object);

        var service = new EnrollmentService(unitOfWork.Object, currentUser.Object);
        return (service, unitOfWork, profile);
    }

    [Fact]
    public async Task CreateEnrollmentAsync_GroundedLearnerReenrolls_RemainsGroundedUntilNewEtrIsCompleted()
    {
        // Re-enrolling alone must NOT clear Grounded — merely being back in a class is not "fit for
        // duty" for aviation certification purposes. Grounded only clears once the new ETR this
        // enrollment creates is actually Completed (see EtrServiceTests.CompleteEtrAsync_* below).
        var classes = new List<Class> { new() { ClassId = 10, CourseId = 1 } };

        var existingEnrollments = new List<CourseEnrollment> { new() { EnrollmentId = 1, AccountId = 100, ClassId = 99 } };
        var existingEtrs = new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 1, EnrollmentId = 1, Status = "Completed", IsLocked = true, IssuedDate = DateTime.UtcNow.AddYears(-2), ExpiryDate = DateTime.UtcNow.AddDays(-1) }
        };
        classes.Add(new Class { ClassId = 99, CourseId = 1 });

        var (service, _, profile) = BuildService(LearnerStatus.Grounded, existingEtrs, existingEnrollments, classes);

        await service.CreateEnrollmentAsync(accountId: 100, classId: 10, createdByAccountId: 1, CancellationToken.None);

        Assert.Equal(LearnerStatus.Grounded, profile.Status);
    }
}
