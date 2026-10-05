using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;

namespace ETR.Application.Tests.Services;

public class EnrollmentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IGenericRepository<Account>> _mockAccountRepo;
    private readonly Mock<IGenericRepository<Class>> _mockClassRepo;
    private readonly Mock<IGenericRepository<Course>> _mockCourseRepo;
    private readonly Mock<IGenericRepository<CourseEnrollment>> _mockEnrollmentRepo;
    private readonly Mock<IETRCourseRecordRepository> _mockEtrRepo;
    private readonly Mock<IGenericRepository<CourseSubject>> _mockCourseSubjectRepo;
    private readonly Mock<IGenericRepository<Subject>> _mockSubjectRepo;
    private readonly Mock<IGenericRepository<SubjectResult>> _mockSubjectResultRepo;
    private readonly Mock<IGenericRepository<Assessment>> _mockAssessmentRepo;
    private readonly Mock<IGenericRepository<AssessmentResult>> _mockAssessmentResultRepo;
    private readonly Mock<IGenericRepository<PracticalChecklist>> _mockChecklistRepo;
    private readonly Mock<IGenericRepository<PracticalChecklistResult>> _mockChecklistResultRepo;
    private readonly Mock<IGenericRepository<CourseDepartment>> _mockCourseDepartmentRepo;
    private readonly Mock<IGenericRepository<Department>> _mockDepartmentRepo;
    private readonly Mock<IGenericRepository<UserProfile>> _mockUserProfileRepo;
    private readonly Mock<IAuditLogRepository> _mockAuditRepo;
    private readonly EnrollmentService _service;

    public EnrollmentServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockAccountRepo = new Mock<IGenericRepository<Account>>();
        _mockClassRepo = new Mock<IGenericRepository<Class>>();
        _mockCourseRepo = new Mock<IGenericRepository<Course>>();
        _mockEnrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        _mockEtrRepo = new Mock<IETRCourseRecordRepository>();
        _mockCourseSubjectRepo = new Mock<IGenericRepository<CourseSubject>>();
        _mockSubjectRepo = new Mock<IGenericRepository<Subject>>();
        _mockSubjectResultRepo = new Mock<IGenericRepository<SubjectResult>>();
        _mockAssessmentRepo = new Mock<IGenericRepository<Assessment>>();
        _mockAssessmentResultRepo = new Mock<IGenericRepository<AssessmentResult>>();
        _mockChecklistRepo = new Mock<IGenericRepository<PracticalChecklist>>();
        _mockChecklistResultRepo = new Mock<IGenericRepository<PracticalChecklistResult>>();
        _mockCourseDepartmentRepo = new Mock<IGenericRepository<CourseDepartment>>();
        _mockDepartmentRepo = new Mock<IGenericRepository<Department>>();
        _mockUserProfileRepo = new Mock<IGenericRepository<UserProfile>>();
        _mockAuditRepo = new Mock<IAuditLogRepository>();

        _mockUow.Setup(u => u.AccountRepository).Returns(_mockAccountRepo.Object);
        _mockUow.Setup(u => u.ClassRepository).Returns(_mockClassRepo.Object);
        _mockUow.Setup(u => u.CourseRepository).Returns(_mockCourseRepo.Object);
        _mockUow.Setup(u => u.CourseEnrollmentRepository).Returns(_mockEnrollmentRepo.Object);
        _mockUow.Setup(u => u.ETRCourseRecordRepository).Returns(_mockEtrRepo.Object);
        _mockUow.Setup(u => u.CourseSubjectRepository).Returns(_mockCourseSubjectRepo.Object);
        _mockUow.Setup(u => u.SubjectRepository).Returns(_mockSubjectRepo.Object);
        _mockUow.Setup(u => u.SubjectResultRepository).Returns(_mockSubjectResultRepo.Object);
        _mockUow.Setup(u => u.AssessmentRepository).Returns(_mockAssessmentRepo.Object);
        _mockUow.Setup(u => u.AssessmentResultRepository).Returns(_mockAssessmentResultRepo.Object);
        _mockUow.Setup(u => u.PracticalChecklistRepository).Returns(_mockChecklistRepo.Object);
        _mockUow.Setup(u => u.PracticalChecklistResultRepository).Returns(_mockChecklistResultRepo.Object);
        _mockUow.Setup(u => u.CourseDepartmentRepository).Returns(_mockCourseDepartmentRepo.Object);
        _mockUow.Setup(u => u.DepartmentRepository).Returns(_mockDepartmentRepo.Object);
        _mockUow.Setup(u => u.UserProfileRepository).Returns(_mockUserProfileRepo.Object);
        _mockUow.Setup(u => u.AuditLogRepository).Returns(_mockAuditRepo.Object);

        _mockCourseDepartmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CourseDepartment>());
        _mockDepartmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Department>());
        _mockUserProfileRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile { AccountId = 50, UserCode = "STU-050", FullName = "Test Student" });

        _service = new EnrollmentService(_mockUow.Object, _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task CreateEnrollmentAsync_ShouldSnapshotSubjectVersionAndCourseVersionNo()
    {
        int studentAccountId = 50;
        int classId = 100;
        int courseId = 200;

        _mockAccountRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account { AccountId = studentAccountId, RoleId = 4, Status = AccountStatus.Active });

        var trainingClass = new Class
        {
            ClassId = classId,
            CourseId = courseId,
            ClassName = "B737 Type Rating 2026-A",
            StartDate = DateTime.UtcNow.AddDays(7),
            Status = ClassStatus.Planned,
            Capacity = 20,
            CourseVersionNo = 2
        };

        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trainingClass);

        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = courseId, CourseCode = "B737", CourseName = "Boeing 737", Status = CourseStatus.Active });

        _mockClassRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Class> { trainingClass });

        _mockEnrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseEnrollment>());

        _mockEtrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ETRCourseRecord>());

        var courseSubjects = new List<CourseSubject>
        {
            new()
            {
                CourseId = courseId,
                SubjectId = 10,
                SequenceNo = 1,
                RequiredHours = 40,
                RequiredSessions = 10,
                IsMandatory = true,
                PassingScore = 75,
                SubjectVersion = "v2.1"
            }
        };

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseSubjects);

        _mockSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject>
            {
                new() { SubjectId = 10, SubjectCode = "GS-101", SubjectName = "Ground School", SubjectType = "Ground" }
            });

        _mockAssessmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assessment>());

        _mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PracticalChecklist>());

        ETRCourseRecord? createdEtr = null;
        _mockEtrRepo.Setup(r => r.AddAsync(It.IsAny<ETRCourseRecord>(), It.IsAny<CancellationToken>()))
            .Callback<ETRCourseRecord, CancellationToken>((etr, _) => createdEtr = etr)
            .Returns(Task.CompletedTask);

        var createdSubjectResults = new List<SubjectResult>();
        _mockSubjectResultRepo.Setup(r => r.AddAsync(It.IsAny<SubjectResult>(), It.IsAny<CancellationToken>()))
            .Callback<SubjectResult, CancellationToken>((sr, _) => createdSubjectResults.Add(sr))
            .Returns(Task.CompletedTask);

        var mockUserProfileRepo = new Mock<IGenericRepository<UserProfile>>();
        mockUserProfileRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { AccountId = studentAccountId, FullName = "Test Student" });
        _mockUow.Setup(u => u.UserProfileRepository).Returns(mockUserProfileRepo.Object);

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<CreateEnrollmentResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CreateEnrollmentResponse>>, CancellationToken>((op, ct) => op(ct));

        var response = await _service.CreateEnrollmentAsync(studentAccountId, classId, createdByAccountId: 99);

        Assert.NotNull(response);
        Assert.NotNull(createdEtr);
        Assert.Equal(2, createdEtr.CourseVersionNo);
        Assert.Single(createdSubjectResults);
        Assert.Equal("v2.1", createdSubjectResults[0].SubjectVersionSnapshot);
        Assert.Equal("GS-101", createdSubjectResults[0].SubjectCodeSnapshot);
        Assert.Equal("Ground School", createdSubjectResults[0].SubjectNameSnapshot);
        Assert.Equal(75, createdSubjectResults[0].PassingScoreSnapshot);
    }

    [Theory]
    [InlineData(ClassStatus.InProgress, 7)]
    [InlineData(ClassStatus.Completed, 7)]
    [InlineData(ClassStatus.Cancelled, 7)]
    [InlineData(ClassStatus.Planned, -2)]
    public async Task CreateEnrollmentAsync_ShouldThrow_WhenClassIsInProgressCompletedCancelledOrPastStartDate(ClassStatus status, int daysFromNow)
    {
        int studentAccountId = 50;
        int classId = 100;
        int courseId = 200;

        var mockUserProfileRepo = new Mock<IGenericRepository<UserProfile>>();
        mockUserProfileRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { AccountId = studentAccountId, FullName = "Test Student" });
        _mockUow.Setup(u => u.UserProfileRepository).Returns(mockUserProfileRepo.Object);

        var trainingClass = new Class
        {
            ClassId = classId,
            CourseId = courseId,
            ClassName = "B737 Type Rating 2026-A",
            StartDate = DateTime.UtcNow.AddDays(daysFromNow),
            Status = status,
            Capacity = 20
        };

        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trainingClass);

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<CreateEnrollmentResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CreateEnrollmentResponse>>, CancellationToken>((op, ct) => op(ct));

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.CreateEnrollmentAsync(studentAccountId, classId, createdByAccountId: 99));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public async Task CreateEnrollmentAsync_ShouldSucceed_WhenClassStartsTodayOrFutureInAcademyTime(int daysFromToday)
    {
        int studentAccountId = 50;
        int classId = 100;
        int courseId = 200;

        _mockAccountRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account { AccountId = studentAccountId, RoleId = 4, Status = AccountStatus.Active });

        var trainingClass = new Class
        {
            ClassId = classId,
            CourseId = courseId,
            ClassName = "B737 Type Rating 2026-Test",
            StartDate = AcademyTimeHelper.GetToday().AddDays(daysFromToday),
            Status = ClassStatus.Planned,
            Capacity = 20,
            CourseVersionNo = 1
        };

        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trainingClass);
        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = courseId, CourseCode = "B737", CourseName = "Boeing 737", Status = CourseStatus.Active });
        _mockClassRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Class> { trainingClass });
        _mockEnrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseEnrollment>());
        _mockEtrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ETRCourseRecord>());

        var courseSubjects = new List<CourseSubject>
        {
            new() { CourseId = courseId, SubjectId = 10, SequenceNo = 1, RequiredHours = 40, RequiredSessions = 10, IsMandatory = true, PassingScore = 75, SubjectVersion = "v1.0" }
        };
        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseSubjects);
        _mockSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject>
            {
                new() { SubjectId = 10, SubjectCode = "GS-101", SubjectName = "Ground School", SubjectType = "Ground" }
            });
        _mockAssessmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assessment>());
        _mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PracticalChecklist>());

        var mockUserProfileRepo = new Mock<IGenericRepository<UserProfile>>();
        mockUserProfileRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { AccountId = studentAccountId, FullName = "Test Student" });
        _mockUow.Setup(u => u.UserProfileRepository).Returns(mockUserProfileRepo.Object);

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<CreateEnrollmentResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CreateEnrollmentResponse>>, CancellationToken>((op, ct) => op(ct));

        var response = await _service.CreateEnrollmentAsync(studentAccountId, classId, createdByAccountId: 99);

        Assert.NotNull(response);
        Assert.Equal(classId, response.ClassId);
    }

    [Theory]
    [InlineData(ClassStatus.InProgress, 7)]
    [InlineData(ClassStatus.Completed, 7)]
    [InlineData(ClassStatus.Cancelled, 7)]
    [InlineData(ClassStatus.Planned, -2)]
    public async Task UpdateEnrollmentAsync_ShouldThrow_WhenTargetClassIsInProgressCompletedCancelledOrPastStartDate(ClassStatus status, int daysFromNow)
    {
        int enrollmentId = 10;
        int currentClassId = 100;
        int targetClassId = 200;
        int studentAccountId = 50;

        var existingEnrollment = new CourseEnrollment
        {
            EnrollmentId = enrollmentId,
            ClassId = currentClassId,
            AccountId = studentAccountId,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAt = DateTime.UtcNow.AddDays(-10)
        };

        var targetClass = new Class
        {
            ClassId = targetClassId,
            CourseId = 1,
            ClassName = "B737 New Batch",
            StartDate = DateTime.UtcNow.AddDays(daysFromNow),
            Status = status,
            Capacity = 20
        };

        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEnrollment);
        _mockClassRepo.Setup(r => r.GetByIdAsync(targetClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetClass);

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<EnrollmentResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<EnrollmentResponse>>, CancellationToken>((op, ct) => op(ct));

        var request = new UpdateEnrollmentRequest(enrollmentId, studentAccountId, targetClassId, EnrollmentStatus.Enrolled, DateTime.UtcNow);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.UpdateEnrollmentAsync(enrollmentId, request, updatedByAccountId: 99));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public async Task UpdateEnrollmentAsync_ShouldSucceed_WhenTargetClassIsPlannedAndStartsTodayOrFuture(int daysFromToday)
    {
        int enrollmentId = 10;
        int currentClassId = 100;
        int targetClassId = 200;
        int studentAccountId = 50;

        var existingEnrollment = new CourseEnrollment
        {
            EnrollmentId = enrollmentId,
            ClassId = currentClassId,
            AccountId = studentAccountId,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAt = DateTime.UtcNow.AddDays(-10)
        };

        var targetClass = new Class
        {
            ClassId = targetClassId,
            CourseId = 1,
            ClassName = "B737 New Batch",
            StartDate = AcademyTimeHelper.GetToday().AddDays(daysFromToday),
            Status = ClassStatus.Planned,
            Capacity = 20
        };

        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEnrollment);
        _mockClassRepo.Setup(r => r.GetByIdAsync(targetClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetClass);

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<EnrollmentResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<EnrollmentResponse>>, CancellationToken>((op, ct) => op(ct));

        var request = new UpdateEnrollmentRequest(enrollmentId, studentAccountId, targetClassId, EnrollmentStatus.Enrolled, DateTime.UtcNow);

        var response = await _service.UpdateEnrollmentAsync(enrollmentId, request, updatedByAccountId: 99);

        Assert.NotNull(response);
        Assert.Equal(targetClassId, response.ClassId);
        _mockEnrollmentRepo.Verify(r => r.Update(existingEnrollment), Times.Once);
    }

    [Fact]
    public async Task CreateEnrollmentAsync_ShouldThrow_WhenStudentDepartmentDoesNotMatchCourseDepartment()
    {
        int studentAccountId = 50;
        int classId = 100;
        int courseId = 200;

        // Student is in Department 4 (Cabin Crew)
        _mockAccountRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account { AccountId = studentAccountId, RoleId = 4, DepartmentId = 4, Status = AccountStatus.Active });

        _mockUserProfileRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { AccountId = studentAccountId, UserCode = "STU-050", FullName = "Cabin Crew Student" });

        var trainingClass = new Class
        {
            ClassId = classId,
            CourseId = courseId,
            ClassName = "Flight Ops Class",
            StartDate = DateTime.UtcNow.AddDays(7),
            Status = ClassStatus.Planned,
            Capacity = 20
        };

        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trainingClass);

        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = courseId, CourseCode = "PILOT-101", CourseName = "Pilot Course", Status = CourseStatus.Active });

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = courseId, SubjectId = 1, SequenceNo = 1, RequiredHours = 10, RequiredSessions = 2, IsMandatory = true } });

        // Course requires Department 3 (Flight Crew)
        _mockCourseDepartmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseDepartment> { new() { CourseId = courseId, DepartmentId = 3, IsDeleted = false } });

        _mockDepartmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department>
            {
                new() { DepartmentId = 3, DepartmentName = "Flight Crew", IsTrainingAudience = true },
                new() { DepartmentId = 4, DepartmentName = "Cabin Crew", IsTrainingAudience = true }
            });

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<CreateEnrollmentResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CreateEnrollmentResponse>>, CancellationToken>((op, ct) => op(ct));

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.CreateEnrollmentAsync(studentAccountId, classId, createdByAccountId: 1));

        Assert.Contains("không thuộc đối tượng đào tạo được phép ghi danh", ex.Message);
        Assert.Contains("Flight Crew", ex.Message);
    }

    [Fact]
    public async Task CreateEnrollmentAsync_ShouldSucceed_WhenStudentDepartmentMatchesCourseDepartment()
    {
        int studentAccountId = 50;
        int classId = 100;
        int courseId = 200;

        // Student is in Department 3 (Flight Crew)
        _mockAccountRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account { AccountId = studentAccountId, RoleId = 4, DepartmentId = 3, Status = AccountStatus.Active });

        _mockUserProfileRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { AccountId = studentAccountId, UserCode = "STU-050", FullName = "Pilot Student" });

        var trainingClass = new Class
        {
            ClassId = classId,
            CourseId = courseId,
            ClassName = "Flight Ops Class",
            StartDate = DateTime.UtcNow.AddDays(7),
            Status = ClassStatus.Planned,
            Capacity = 20
        };

        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trainingClass);

        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = courseId, CourseCode = "PILOT-101", CourseName = "Pilot Course", Status = CourseStatus.Active });

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = courseId, SubjectId = 1, SequenceNo = 1, RequiredHours = 10, RequiredSessions = 2, IsMandatory = true } });

        // Course requires Department 3 (Flight Crew)
        _mockCourseDepartmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseDepartment> { new() { CourseId = courseId, DepartmentId = 3, IsDeleted = false } });

        _mockAssessmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assessment>());
        _mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PracticalChecklist>());
        _mockSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject> { new() { SubjectId = 1, SubjectCode = "S1", SubjectName = "Subject 1" } });

        _mockEnrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseEnrollment>());

        _mockEtrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ETRCourseRecord>());

        _mockClassRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Class> { trainingClass });

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<CreateEnrollmentResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CreateEnrollmentResponse>>, CancellationToken>((op, ct) => op(ct));

        var response = await _service.CreateEnrollmentAsync(studentAccountId, classId, createdByAccountId: 1);

        Assert.NotNull(response);
        _mockEnrollmentRepo.Verify(r => r.AddAsync(It.IsAny<CourseEnrollment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEnrollmentAsync_ShouldThrow_WhenStudentDepartmentDoesNotMatchTargetClassCourseDepartment()
    {
        int enrollmentId = 10;
        int currentClassId = 100;
        int targetClassId = 200;
        int targetCourseId = 500;
        int studentAccountId = 50;

        var existingEnrollment = new CourseEnrollment
        {
            EnrollmentId = enrollmentId,
            ClassId = currentClassId,
            AccountId = studentAccountId,
            Status = EnrollmentStatus.Enrolled,
            EnrolledAt = DateTime.UtcNow.AddDays(-10)
        };

        var targetClass = new Class
        {
            ClassId = targetClassId,
            CourseId = targetCourseId,
            ClassName = "Flight Ops Advanced",
            StartDate = AcademyTimeHelper.GetToday().AddDays(5),
            Status = ClassStatus.Planned,
            Capacity = 20
        };

        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEnrollment);
        _mockClassRepo.Setup(r => r.GetByIdAsync(targetClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetClass);

        // Target Course allows Department 3 (Flight Crew)
        _mockCourseDepartmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseDepartment> { new() { CourseId = targetCourseId, DepartmentId = 3, IsDeleted = false } });

        // Student is in Department 6 (Ground Ops)
        _mockAccountRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account { AccountId = studentAccountId, RoleId = 4, DepartmentId = 6, Status = AccountStatus.Active });

        _mockDepartmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department>
            {
                new() { DepartmentId = 3, DepartmentName = "Flight Crew" },
                new() { DepartmentId = 6, DepartmentName = "Ground Operations" }
            });

        _mockUserProfileRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { AccountId = studentAccountId, UserCode = "STU-050", FullName = "Ground Ops Student" });

        _mockCourseRepo.Setup(r => r.GetByIdAsync(targetCourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = targetCourseId, CourseCode = "FLT-ADV", CourseName = "Flight Ops Advanced" });

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<EnrollmentResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<EnrollmentResponse>>, CancellationToken>((op, ct) => op(ct));

        var request = new UpdateEnrollmentRequest(enrollmentId, studentAccountId, targetClassId, EnrollmentStatus.Enrolled, DateTime.UtcNow);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.UpdateEnrollmentAsync(enrollmentId, request, updatedByAccountId: 99));

        Assert.Contains("không thuộc đối tượng đào tạo của khóa học", ex.Message);
        Assert.Contains("Flight Crew", ex.Message);
    }

    [Fact]
    public async Task CreateEnrollmentAsync_AllowsEnrollment_WhenCourseAllowsMultipleDepartmentsAndStudentIsInOne()
    {
        int classId = 10;
        int courseId = 20;
        int studentAccountId = 30;

        var cls = new Class
        {
            ClassId = classId,
            CourseId = courseId,
            ClassName = "Safety Training",
            StartDate = AcademyTimeHelper.GetToday().AddDays(10),
            Status = ClassStatus.Planned,
            Capacity = 30
        };

        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cls);

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = courseId, SubjectId = 101, IsDeleted = false } });

        // Course allows Dept 3 (Flight Crew) and Dept 4 (Cabin Crew)
        _mockCourseDepartmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseDepartment>
            {
                new() { CourseId = courseId, DepartmentId = 3, IsDeleted = false },
                new() { CourseId = courseId, DepartmentId = 4, IsDeleted = false }
            });

        // Student is in Dept 4 (Cabin Crew)
        _mockAccountRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account { AccountId = studentAccountId, RoleId = 4, DepartmentId = 4, Status = AccountStatus.Active });

        _mockUserProfileRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { AccountId = studentAccountId, UserCode = "STU-CC-01", FullName = "Cabin Crew Student" });

        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = courseId, CourseCode = "SAF-01", CourseName = "Safety Training" });

        _mockEnrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseEnrollment>());

        _mockEtrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ETRCourseRecord>());

        _mockAssessmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assessment>());

        _mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PracticalChecklist>());

        _mockSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject> { new() { SubjectId = 101, SubjectCode = "SAF-101", SubjectName = "Safety", SubjectType = "Ground" } });

        _mockSubjectResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectResult>());

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<CreateEnrollmentResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CreateEnrollmentResponse>>, CancellationToken>((op, ct) => op(ct));

        var result = await _service.CreateEnrollmentAsync(studentAccountId, classId, createdByAccountId: 99);

        Assert.NotNull(result);
        _mockEnrollmentRepo.Verify(r => r.AddAsync(It.IsAny<CourseEnrollment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateEnrollmentAsync_AllowsEnrollment_WhenCourseHasNoDepartmentRestrictions()
    {
        int classId = 15;
        int courseId = 25;
        int studentAccountId = 35;

        var cls = new Class
        {
            ClassId = classId,
            CourseId = courseId,
            ClassName = "General Aviation Basics",
            StartDate = AcademyTimeHelper.GetToday().AddDays(10),
            Status = ClassStatus.Planned,
            Capacity = 30
        };

        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cls);

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = courseId, SubjectId = 101, IsDeleted = false } });

        // Unrestricted course: 0 CourseDepartments configured
        _mockCourseDepartmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseDepartment>());

        // Student with default/unassigned department 0
        _mockAccountRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account { AccountId = studentAccountId, RoleId = 4, DepartmentId = 0, Status = AccountStatus.Active });

        _mockUserProfileRepo.Setup(r => r.GetByIdAsync(studentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { AccountId = studentAccountId, UserCode = "STU-GEN-01", FullName = "General Student" });

        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = courseId, CourseCode = "GEN-01", CourseName = "General Aviation Basics" });

        _mockEnrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseEnrollment>());

        _mockEtrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ETRCourseRecord>());

        _mockAssessmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assessment>());

        _mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PracticalChecklist>());

        _mockSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject> { new() { SubjectId = 101, SubjectCode = "GEN-101", SubjectName = "Gen Aviation", SubjectType = "Ground" } });

        _mockSubjectResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectResult>());

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<CreateEnrollmentResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CreateEnrollmentResponse>>, CancellationToken>((op, ct) => op(ct));

        var result = await _service.CreateEnrollmentAsync(studentAccountId, classId, createdByAccountId: 99);

        Assert.NotNull(result);
        _mockEnrollmentRepo.Verify(r => r.AddAsync(It.IsAny<CourseEnrollment>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
