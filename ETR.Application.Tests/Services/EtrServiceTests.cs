using ETR.Application.Compliance;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using Moq;

namespace ETR.Application.Tests.Services;

public class EtrServiceTests
{
    private static (EtrService Service, Mock<IUnitOfWork> UnitOfWork) BuildService()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.RoleName).Returns("Admin");
        currentUser.SetupGet(c => c.AccountId).Returns(1);

        var auditLogRepo = new Mock<IAuditLogRepository>();
        unitOfWork.SetupGet(u => u.AuditLogRepository).Returns(auditLogRepo.Object);

        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Course>());
        unitOfWork.SetupGet(u => u.CourseRepository).Returns(courseRepo.Object);

        var accountRepo = new Mock<IGenericRepository<Account>>();
        accountRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Account>());
        unitOfWork.SetupGet(u => u.AccountRepository).Returns(accountRepo.Object);

        var service = new EtrService(unitOfWork.Object, currentUser.Object);
        return (service, unitOfWork);
    }

    private static void SetupLearnerData(
        Mock<IUnitOfWork> unitOfWork,
        List<UserProfile> profiles,
        List<CourseEnrollment> enrollments,
        List<Class> classes,
        List<ETRCourseRecord> etrs)
    {
        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
        unitOfWork.SetupGet(u => u.UserProfileRepository).Returns(profileRepo.Object);

        var enrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(enrollments);
        unitOfWork.SetupGet(u => u.CourseEnrollmentRepository).Returns(enrollmentRepo.Object);

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(classes);
        unitOfWork.SetupGet(u => u.ClassRepository).Returns(classRepo.Object);

        var etrRepo = new Mock<IETRCourseRecordRepository>();
        etrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(etrs);
        unitOfWork.SetupGet(u => u.ETRCourseRecordRepository).Returns(etrRepo.Object);
    }

    [Fact]
    public async Task RefreshGroundedStatusAsync_LearnerWithExpiredCompletedEtr_BecomesGrounded()
    {
        var (service, unitOfWork) = BuildService();

        var profiles = new List<UserProfile> { new() { AccountId = 100, Status = LearnerStatus.Active, UserCode = "U100", FullName = "Learner 100" } };
        var enrollments = new List<CourseEnrollment> { new() { EnrollmentId = 1, AccountId = 100, ClassId = 10 } };
        var classes = new List<Class> { new() { ClassId = 10, CourseId = 1 } };
        var etrs = new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 1, EnrollmentId = 1, Status = "Completed", IssuedDate = DateTime.UtcNow.AddYears(-2), ExpiryDate = DateTime.UtcNow.AddDays(-1) }
        };
        SetupLearnerData(unitOfWork, profiles, enrollments, classes, etrs);

        var result = await service.RefreshGroundedStatusAsync(actorAccountId: 1, CancellationToken.None);

        Assert.Equal(1, result.ScannedCount);
        Assert.Equal(1, result.GroundedCount);
        Assert.Equal(0, result.ClearedCount);
        Assert.Equal(LearnerStatus.Grounded, profiles[0].Status);
    }

    [Fact]
    public async Task RefreshGroundedStatusAsync_GroundedLearnerWithNoExpiredEtr_ClearsBackToActive()
    {
        var (service, unitOfWork) = BuildService();

        var profiles = new List<UserProfile> { new() { AccountId = 200, Status = LearnerStatus.Grounded, UserCode = "U200", FullName = "Learner 200" } };
        var enrollments = new List<CourseEnrollment> { new() { EnrollmentId = 2, AccountId = 200, ClassId = 20 } };
        var classes = new List<Class> { new() { ClassId = 20, CourseId = 2 } };
        var etrs = new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 2, EnrollmentId = 2, Status = "InProgress", ExpiryDate = null }
        };
        SetupLearnerData(unitOfWork, profiles, enrollments, classes, etrs);

        var result = await service.RefreshGroundedStatusAsync(actorAccountId: 1, CancellationToken.None);

        Assert.Equal(0, result.GroundedCount);
        Assert.Equal(1, result.ClearedCount);
        Assert.Equal(LearnerStatus.Active, profiles[0].Status);
    }

    [Fact]
    public async Task RefreshGroundedStatusAsync_WithdrawnLearner_IsNeverTouchedEvenIfExpired()
    {
        var (service, unitOfWork) = BuildService();

        var profiles = new List<UserProfile> { new() { AccountId = 300, Status = LearnerStatus.Withdrawn, UserCode = "U300", FullName = "Learner 300" } };
        var enrollments = new List<CourseEnrollment> { new() { EnrollmentId = 3, AccountId = 300, ClassId = 30 } };
        var classes = new List<Class> { new() { ClassId = 30, CourseId = 3 } };
        var etrs = new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 3, EnrollmentId = 3, Status = "Completed", IssuedDate = DateTime.UtcNow.AddYears(-2), ExpiryDate = DateTime.UtcNow.AddDays(-1) }
        };
        SetupLearnerData(unitOfWork, profiles, enrollments, classes, etrs);

        var result = await service.RefreshGroundedStatusAsync(actorAccountId: 1, CancellationToken.None);

        Assert.Equal(0, result.ScannedCount);
        Assert.Equal(LearnerStatus.Withdrawn, profiles[0].Status);
    }

    [Fact]
    public async Task GetDueForTrainingAsync_NoCourseIdGiven_ScansEveryCourse()
    {
        var (service, unitOfWork) = BuildService();

        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Course>
        {
            new() { CourseId = 1, CourseName = "Safety" },
            new() { CourseId = 2, CourseName = "First Aid" }
        });
        unitOfWork.SetupGet(u => u.CourseRepository).Returns(courseRepo.Object);

        var classes = new List<Class>
        {
            new() { ClassId = 10, CourseId = 1 },
            new() { ClassId = 20, CourseId = 2 }
        };
        var enrollments = new List<CourseEnrollment>
        {
            new() { EnrollmentId = 1, AccountId = 100, ClassId = 10 },
            new() { EnrollmentId = 2, AccountId = 200, ClassId = 20 }
        };
        var etrs = new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 1, EnrollmentId = 1, Status = "Completed", IssuedDate = DateTime.UtcNow.AddYears(-1), ExpiryDate = DateTime.UtcNow.AddDays(-1) },
            new() { ETRCourseRecordId = 2, EnrollmentId = 2, Status = "Completed", IssuedDate = DateTime.UtcNow.AddYears(-1), ExpiryDate = DateTime.UtcNow.AddDays(90) }
        };

        var enrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(enrollments);
        unitOfWork.SetupGet(u => u.CourseEnrollmentRepository).Returns(enrollmentRepo.Object);

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(classes);
        unitOfWork.SetupGet(u => u.ClassRepository).Returns(classRepo.Object);

        var etrRepo = new Mock<IETRCourseRecordRepository>();
        etrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(etrs);
        unitOfWork.SetupGet(u => u.ETRCourseRecordRepository).Returns(etrRepo.Object);

        var accountRepo = new Mock<IGenericRepository<Account>>();
        accountRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Account>
        {
            new() { AccountId = 100, Username = "u100" },
            new() { AccountId = 200, Username = "u200" }
        });
        unitOfWork.SetupGet(u => u.AccountRepository).Returns(accountRepo.Object);

        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserProfile>());
        unitOfWork.SetupGet(u => u.UserProfileRepository).Returns(profileRepo.Object);

        var result = (await service.GetDueForTrainingAsync(courseId: null, daysThreshold: 30, CancellationToken.None)).ToList();

        Assert.Single(result);
        Assert.Equal(100, result[0].AccountId);
        Assert.Equal("Expired", result[0].ValidityStatus);
    }

    private static Mock<IUnitOfWork> BuildCompleteEtrDependencies(
        Mock<IUnitOfWork> unitOfWork,
        ETRCourseRecord etrBeingCompleted,
        CourseEnrollment enrollment,
        Class trainingClass,
        Course course,
        UserProfile learnerProfile,
        List<CourseEnrollment> allEnrollments,
        List<Class> allClasses,
        List<ETRCourseRecord> allEtrs)
    {
        var etrRepo = new Mock<IETRCourseRecordRepository>();
        etrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrBeingCompleted.ETRCourseRecordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(etrBeingCompleted);
        etrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => allEtrs);
        unitOfWork.SetupGet(u => u.ETRCourseRecordRepository).Returns(etrRepo.Object);

        var enrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrollmentRepo.Setup(r => r.GetByIdAsync(enrollment.EnrollmentId, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);
        enrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => allEnrollments);
        unitOfWork.SetupGet(u => u.CourseEnrollmentRepository).Returns(enrollmentRepo.Object);

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetByIdAsync(trainingClass.ClassId, It.IsAny<CancellationToken>())).ReturnsAsync(trainingClass);
        classRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => allClasses);
        unitOfWork.SetupGet(u => u.ClassRepository).Returns(classRepo.Object);

        var courseSubjectRepo = new Mock<IGenericRepository<CourseSubject>>();
        courseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = course.CourseId, SubjectId = 1, IsMandatory = true } });
        unitOfWork.SetupGet(u => u.CourseSubjectRepository).Returns(courseSubjectRepo.Object);

        var evidenceRepo = new Mock<IGenericRepository<EvidenceFile>>();
        evidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<EvidenceFile>());
        unitOfWork.SetupGet(u => u.EvidenceFileRepository).Returns(evidenceRepo.Object);

        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetByIdAsync(course.CourseId, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        unitOfWork.SetupGet(u => u.CourseRepository).Returns(courseRepo.Object);

        var approvalRequestRepo = new Mock<IGenericRepository<ApprovalRequest>>();
        approvalRequestRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApprovalRequest>());
        unitOfWork.SetupGet(u => u.ApprovalRequestRepository).Returns(approvalRequestRepo.Object);

        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetByIdAsync(learnerProfile.AccountId, It.IsAny<CancellationToken>())).ReturnsAsync(learnerProfile);
        unitOfWork.SetupGet(u => u.UserProfileRepository).Returns(profileRepo.Object);

        var auditLogRepo = new Mock<IAuditLogRepository>();
        unitOfWork.SetupGet(u => u.AuditLogRepository).Returns(auditLogRepo.Object);

        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        return unitOfWork;
    }

    [Fact]
    public async Task CompleteEtrAsync_GroundedLearnerWithNoOtherExpiredCourse_ClearsBackToActive()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.RoleName).Returns("Admin");
        currentUser.SetupGet(c => c.AccountId).Returns(1);

        var course = new Course { CourseId = 1, CourseName = "Safety", ValidityMonths = 12 };
        var oldClass = new Class { ClassId = 99, CourseId = 1 };
        var newClass = new Class { ClassId = 10, CourseId = 1 };
        var newEnrollment = new CourseEnrollment { EnrollmentId = 2, AccountId = 100, ClassId = 10 };
        var oldEnrollment = new CourseEnrollment { EnrollmentId = 1, AccountId = 100, ClassId = 99 };
        var learnerProfile = new UserProfile { AccountId = 100, Status = LearnerStatus.Grounded, UserCode = "U100", FullName = "Learner 100" };

        var oldEtr = new ETRCourseRecord { ETRCourseRecordId = 1, EnrollmentId = 1, Status = "Completed", IsLocked = true, IssuedDate = DateTime.UtcNow.AddYears(-2), ExpiryDate = DateTime.UtcNow.AddDays(-1) };
        var newEtr = new ETRCourseRecord
        {
            ETRCourseRecordId = 2,
            EnrollmentId = 2,
            Status = "Verified",
            IsLocked = false,
            SubjectResults = new List<SubjectResult> { new() { SubjectResultId = 1, SubjectId = 1, Status = "Passed" } }
        };

        var allEnrollments = new List<CourseEnrollment> { oldEnrollment, newEnrollment };
        var allClasses = new List<Class> { oldClass, newClass };
        var allEtrs = new List<ETRCourseRecord> { oldEtr, newEtr };

        BuildCompleteEtrDependencies(unitOfWork, newEtr, newEnrollment, newClass, course, learnerProfile, allEnrollments, allClasses, allEtrs);

        var service = new EtrService(unitOfWork.Object, currentUser.Object);

        await service.CompleteEtrAsync(newEtr.ETRCourseRecordId, accountId: 1, CancellationToken.None);

        Assert.Equal("Completed", newEtr.Status);
        Assert.Equal(LearnerStatus.Active, learnerProfile.Status);
    }

    [Fact]
    public async Task CompleteEtrAsync_GroundedLearnerHasAnotherStillExpiredCourse_RemainsGrounded()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.RoleName).Returns("Admin");
        currentUser.SetupGet(c => c.AccountId).Returns(1);

        var course1 = new Course { CourseId = 1, CourseName = "Safety", ValidityMonths = 12 };
        var classForCourse1 = new Class { ClassId = 10, CourseId = 1 };
        var enrollmentForCourse1 = new CourseEnrollment { EnrollmentId = 2, AccountId = 100, ClassId = 10 };

        // A different course (2) still has an expired completed ETR, unrelated to the one being completed.
        var classForCourse2 = new Class { ClassId = 88, CourseId = 2 };
        var enrollmentForCourse2 = new CourseEnrollment { EnrollmentId = 1, AccountId = 100, ClassId = 88 };
        var expiredEtrForCourse2 = new ETRCourseRecord { ETRCourseRecordId = 1, EnrollmentId = 1, Status = "Completed", IsLocked = true, IssuedDate = DateTime.UtcNow.AddYears(-2), ExpiryDate = DateTime.UtcNow.AddDays(-1) };

        var learnerProfile = new UserProfile { AccountId = 100, Status = LearnerStatus.Grounded, UserCode = "U100", FullName = "Learner 100" };

        var newEtr = new ETRCourseRecord
        {
            ETRCourseRecordId = 2,
            EnrollmentId = 2,
            Status = "Verified",
            IsLocked = false,
            SubjectResults = new List<SubjectResult> { new() { SubjectResultId = 1, SubjectId = 1, Status = "Passed" } }
        };

        var allEnrollments = new List<CourseEnrollment> { enrollmentForCourse2, enrollmentForCourse1 };
        var allClasses = new List<Class> { classForCourse2, classForCourse1 };
        var allEtrs = new List<ETRCourseRecord> { expiredEtrForCourse2, newEtr };

        BuildCompleteEtrDependencies(unitOfWork, newEtr, enrollmentForCourse1, classForCourse1, course1, learnerProfile, allEnrollments, allClasses, allEtrs);

        var service = new EtrService(unitOfWork.Object, currentUser.Object);

        await service.CompleteEtrAsync(newEtr.ETRCourseRecordId, accountId: 1, CancellationToken.None);

        Assert.Equal("Completed", newEtr.Status);
        Assert.Equal(LearnerStatus.Grounded, learnerProfile.Status);
    }

    [Fact]
    public async Task GetCompletionProgressAsync_UsesCompletionRequirementMatchingEtrCourseVersionNo_NotTheLatestOne()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUserService>();

        // Learner enrolled while MinAttendance was still 80% (VersionNo=1). The rule was later
        // tightened to 95% (VersionNo=2) — this ETR must keep evaluating against the 80% rule that
        // was in force when it was created, not the new 95% one.
        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = 1,
            EnrollmentId = 1,
            CourseVersionNo = 1,
            SubjectResults = new List<SubjectResult> { new() { SubjectResultId = 1, SubjectId = 1, Status = "Passed", AttendanceRate = 85m } }
        };
        var etrRepo = new Mock<IETRCourseRecordRepository>();
        etrRepo.Setup(r => r.GetWithSubjectResultsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(etr);
        unitOfWork.SetupGet(u => u.ETRCourseRecordRepository).Returns(etrRepo.Object);

        var enrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrollmentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new CourseEnrollment { EnrollmentId = 1, ClassId = 10 });
        unitOfWork.SetupGet(u => u.CourseEnrollmentRepository).Returns(enrollmentRepo.Object);

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new Class { ClassId = 10, CourseId = 1 });
        unitOfWork.SetupGet(u => u.ClassRepository).Returns(classRepo.Object);

        var courseSubjectRepo = new Mock<IGenericRepository<CourseSubject>>();
        courseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = 1, SubjectId = 1, IsMandatory = true } });
        unitOfWork.SetupGet(u => u.CourseSubjectRepository).Returns(courseSubjectRepo.Object);

        var subjectRepo = new Mock<IGenericRepository<Subject>>();
        subjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject> { new() { SubjectId = 1, SubjectName = "Safety" } });
        unitOfWork.SetupGet(u => u.SubjectRepository).Returns(subjectRepo.Object);

        var evidenceRepo = new Mock<IGenericRepository<EvidenceFile>>();
        evidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<EvidenceFile>());
        unitOfWork.SetupGet(u => u.EvidenceFileRepository).Returns(evidenceRepo.Object);

        var signoffRepo = new Mock<IGenericRepository<SubjectSignoff>>();
        signoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SubjectSignoff> { new() { SubjectResultId = 1 } });
        unitOfWork.SetupGet(u => u.SubjectSignoffRepository).Returns(signoffRepo.Object);

        var checklistRepo = new Mock<IGenericRepository<PracticalChecklist>>();
        checklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklist>());
        unitOfWork.SetupGet(u => u.PracticalChecklistRepository).Returns(checklistRepo.Object);

        var checklistResultRepo = new Mock<IGenericRepository<PracticalChecklistResult>>();
        checklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklistResult>());
        unitOfWork.SetupGet(u => u.PracticalChecklistResultRepository).Returns(checklistResultRepo.Object);

        var requirementRepo = new Mock<IGenericRepository<ETR.Domain.Entities.CompletionRequirement>>();
        requirementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ETR.Domain.Entities.CompletionRequirement>
        {
            new() { RequirementId = 1, CourseId = 1, RequirementName = "Min Attendance", RequirementType = "MinAttendance", ThresholdValue = 80m, IsMandatory = true, VersionNo = 1, EffectiveTo = DateTime.UtcNow.AddDays(-1) },
            new() { RequirementId = 2, CourseId = 1, RequirementName = "Min Attendance", RequirementType = "MinAttendance", ThresholdValue = 95m, IsMandatory = true, VersionNo = 2, EffectiveTo = null },
        });
        unitOfWork.SetupGet(u => u.CompletionRequirementRepository).Returns(requirementRepo.Object);

        var service = new EtrService(unitOfWork.Object, currentUser.Object);

        var result = await service.GetCompletionProgressAsync(1, CancellationToken.None);

        var requirementChecks = result.Checks.Where(c => c.Name.StartsWith("Completion Requirement:")).ToList();
        Assert.Single(requirementChecks);
        Assert.True(requirementChecks[0].IsMet); // 85% passes the VersionNo=1 (80%) rule the learner enrolled under, not the newer 95% one.
    }
}
