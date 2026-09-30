using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;

namespace ETR.Application.Tests.Services;

public class CourseServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IGenericRepository<Course>> _mockCourseRepo;
    private readonly Mock<IGenericRepository<Subject>> _mockSubjectRepo;
    private readonly Mock<IGenericRepository<CourseSubject>> _mockCourseSubjectRepo;
    private readonly Mock<IGenericRepository<Class>> _mockClassRepo;
    private readonly Mock<IGenericRepository<ClassSubject>> _mockClassSubjectRepo;
    private readonly Mock<IAuditLogRepository> _mockAuditRepo;
    private readonly CourseService _service;

    public CourseServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockCourseRepo = new Mock<IGenericRepository<Course>>();
        _mockSubjectRepo = new Mock<IGenericRepository<Subject>>();
        _mockCourseSubjectRepo = new Mock<IGenericRepository<CourseSubject>>();
        _mockClassRepo = new Mock<IGenericRepository<Class>>();
        _mockClassSubjectRepo = new Mock<IGenericRepository<ClassSubject>>();
        _mockAuditRepo = new Mock<IAuditLogRepository>();

        _mockUow.Setup(u => u.CourseRepository).Returns(_mockCourseRepo.Object);
        _mockUow.Setup(u => u.SubjectRepository).Returns(_mockSubjectRepo.Object);
        _mockUow.Setup(u => u.CourseSubjectRepository).Returns(_mockCourseSubjectRepo.Object);
        _mockUow.Setup(u => u.ClassRepository).Returns(_mockClassRepo.Object);
        _mockUow.Setup(u => u.ClassSubjectRepository).Returns(_mockClassSubjectRepo.Object);
        _mockUow.Setup(u => u.AuditLogRepository).Returns(_mockAuditRepo.Object);

        _service = new CourseService(_mockUow.Object);
    }

    [Fact]
    public async Task RemoveSubjectFromCourseAsync_ShouldThrow_WhenOngoingClassTeachesSubject()
    {
        int courseId = 10;
        int subjectId = 20;

        var mapping = new CourseSubject { CourseId = courseId, SubjectId = subjectId, IsDeleted = false };
        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { mapping });

        var classes = new List<Class>
        {
            new() { ClassId = 101, CourseId = courseId, ClassCode = "PILOT-2026-A", Status = ClassStatus.InProgress, IsDeleted = false }
        };
        _mockClassRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(classes);

        var classSubjects = new List<ClassSubject>
        {
            new() { ClassId = 101, SubjectId = subjectId, IsDeleted = false }
        };
        _mockClassSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(classSubjects);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.RemoveSubjectFromCourseAsync(courseId, subjectId, deletedByAccountId: 1));

        Assert.Contains("PILOT-2026-A", ex.Message);
        Assert.Contains("chưa kết thúc", ex.Message);
    }

    [Fact]
    public async Task UpdateCourseSubjectAsync_ShouldThrow_WhenChangingPassingScoreDuringOngoingClass()
    {
        int courseId = 10;
        int subjectId = 20;

        var mapping = new CourseSubject
        {
            CourseId = courseId,
            SubjectId = subjectId,
            PassingScore = 75,
            RequiredHours = 40,
            RequiredSessions = 10,
            IsMandatory = true,
            SequenceNo = 1
        };
        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { mapping });

        var classes = new List<Class>
        {
            new() { ClassId = 101, CourseId = courseId, ClassCode = "PILOT-2026-B", Status = ClassStatus.InProgress, IsDeleted = false }
        };
        _mockClassRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(classes);

        var classSubjects = new List<ClassSubject>
        {
            new() { ClassId = 101, SubjectId = subjectId, IsDeleted = false }
        };
        _mockClassSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(classSubjects);

        // Attempt to increase passing score from 75 to 80
        var updateRequest = new UpdateCourseSubjectRequest
        {
            SequenceNo = 1,
            RequiredHours = 40,
            RequiredSessions = 10,
            IsMandatory = true,
            PassingScore = 80
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.UpdateCourseSubjectAsync(courseId, subjectId, updateRequest, updatedByAccountId: 1));

        Assert.Contains("Không thể thay đổi tiêu chí đánh giá môn học", ex.Message);
        Assert.Contains("PILOT-2026-B", ex.Message);
    }

    [Fact]
    public async Task UpdateCourseSubjectAsync_ShouldSucceed_WhenOnlySequenceNoChangesDuringOngoingClass()
    {
        int courseId = 10;
        int subjectId = 20;

        var mapping = new CourseSubject
        {
            CourseId = courseId,
            SubjectId = subjectId,
            PassingScore = 75,
            RequiredHours = 40,
            RequiredSessions = 10,
            IsMandatory = true,
            SequenceNo = 1
        };
        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { mapping });

        var classes = new List<Class>
        {
            new() { ClassId = 101, CourseId = courseId, ClassCode = "PILOT-2026-B", Status = ClassStatus.InProgress, IsDeleted = false }
        };
        _mockClassRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(classes);

        var classSubjects = new List<ClassSubject>
        {
            new() { ClassId = 101, SubjectId = subjectId, IsDeleted = false }
        };
        _mockClassSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(classSubjects);

        // Only re-ordering SequenceNo (1 -> 2)
        var updateRequest = new UpdateCourseSubjectRequest
        {
            SequenceNo = 2,
            RequiredHours = 40,
            RequiredSessions = 10,
            IsMandatory = true,
            PassingScore = 75
        };

        var result = await _service.UpdateCourseSubjectAsync(courseId, subjectId, updateRequest, updatedByAccountId: 1);

        Assert.Equal(2, result.SequenceNo);
        _mockCourseSubjectRepo.Verify(r => r.Update(mapping), Times.Once);
        _mockUow.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddSubjectToCourseAsync_ShouldReactivate_WhenSoftDeletedMappingExists()
    {
        int courseId = 10;
        int subjectId = 30;

        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = courseId, CourseName = "Aviation 101" });
        _mockSubjectRepo.Setup(r => r.GetByIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subject { SubjectId = subjectId, SubjectName = "Navigation" });

        var existingSoftDeleted = new CourseSubject
        {
            CourseId = courseId,
            SubjectId = subjectId,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-5),
            PassingScore = 60
        };

        _mockCourseSubjectRepo.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { existingSoftDeleted });

        var request = new AddCourseSubjectRequest
        {
            SubjectId = subjectId,
            SequenceNo = 3,
            RequiredHours = 30,
            RequiredSessions = 8,
            IsMandatory = true,
            PassingScore = 80
        };

        var result = await _service.AddSubjectToCourseAsync(courseId, request, addedByAccountId: 1);

        Assert.False(existingSoftDeleted.IsDeleted);
        Assert.Null(existingSoftDeleted.DeletedAt);
        Assert.Equal(80, result.PassingScore);
        Assert.Equal(3, result.SequenceNo);

        _mockCourseSubjectRepo.Verify(r => r.Update(existingSoftDeleted), Times.Once);
        _mockUow.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureCourseNotLockedAsync_ShouldThrow_WhenCourseHasScheduledClass()
    {
        int courseId = 10;
        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = courseId, CourseCode = "C101", CourseName = "Private Pilot", VersionNo = 1 });

        _mockClassRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Class>
            {
                new() { ClassId = 1, CourseId = courseId, Status = ClassStatus.Scheduled, IsDeleted = false }
            });

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.EnsureCourseNotLockedAsync(courseId));

        Assert.Contains("đóng băng bất biến", ex.Message);
    }

    [Fact]
    public async Task CloneCourseVersionAsync_ShouldCloneCourseAndBumpVersionNo()
    {
        int courseId = 10;
        int createdByAccountId = 99;

        var originalCourse = new Course
        {
            CourseId = courseId,
            CourseCode = "PPL-2026",
            CourseName = "Private Pilot License",
            Description = "Initial Pilot Training",
            DurationHours = 120,
            Status = CourseStatus.Active,
            VersionNo = 1
        };

        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalCourse);

        _mockCourseRepo.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Course> { originalCourse });

        var mockAssessmentRepo = new Mock<IGenericRepository<Assessment>>();
        var mockChecklistRepo = new Mock<IGenericRepository<PracticalChecklist>>();
        var mockReqRepo = new Mock<IGenericRepository<CompletionRequirement>>();
        var mockEnrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();

        _mockUow.Setup(u => u.AssessmentRepository).Returns(mockAssessmentRepo.Object);
        _mockUow.Setup(u => u.PracticalChecklistRepository).Returns(mockChecklistRepo.Object);
        _mockUow.Setup(u => u.CompletionRequirementRepository).Returns(mockReqRepo.Object);
        _mockUow.Setup(u => u.CourseEnrollmentRepository).Returns(mockEnrollmentRepo.Object);

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<CourseResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CourseResponse>>, CancellationToken>((op, ct) => op(ct));

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject>
            {
                new() { CourseId = courseId, SubjectId = 1, SequenceNo = 1, PassingScore = 80, IsMandatory = true }
            });

        mockAssessmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Assessment>
            {
                new() { CourseId = courseId, SubjectId = 1, ComponentName = "Midterm", Weight = 40, PassingScore = 80, IsRequired = true }
            });

        mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PracticalChecklist>
            {
                new() { CourseId = courseId, SubjectId = 1, ItemName = "Pre-flight Inspection", IsRequired = true }
            });

        mockReqRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompletionRequirement>
            {
                new() { CourseId = courseId, RequirementName = "Min Attendance 80%", RequirementType = "MinAttendance", ThresholdValue = 80, IsMandatory = true, VersionNo = 1 }
            });

        var result = await _service.CloneCourseVersionAsync(courseId, createdByAccountId);

        Assert.Equal(2, result.VersionNo);
        Assert.Equal("PPL-2026", result.CourseCode);
        Assert.Equal(courseId, result.PreviousVersionId);
        Assert.Equal(CourseStatus.Draft, result.Status);
        Assert.Single(result.Subjects!);

        _mockCourseRepo.Verify(r => r.AddAsync(It.Is<Course>(c => c.VersionNo == 2 && c.PreviousVersionId == courseId && c.Status == CourseStatus.Draft), It.IsAny<CancellationToken>()), Times.Once);
        _mockCourseSubjectRepo.Verify(r => r.AddAsync(It.Is<CourseSubject>(cs => cs.SubjectId == 1 && cs.PassingScore == 80), It.IsAny<CancellationToken>()), Times.Once);
        mockAssessmentRepo.Verify(r => r.AddAsync(It.Is<Assessment>(a => a.ComponentName == "Midterm"), It.IsAny<CancellationToken>()), Times.Once);
        mockChecklistRepo.Verify(r => r.AddAsync(It.Is<PracticalChecklist>(p => p.ItemName == "Pre-flight Inspection"), It.IsAny<CancellationToken>()), Times.Once);
        mockReqRepo.Verify(r => r.AddAsync(It.Is<CompletionRequirement>(cr => cr.VersionNo == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCourseAsync_ShouldThrow_WhenModifyingSubjectsOfLockedCourse()
    {
        int courseId = 10;
        var existingCourse = new Course
        {
            CourseId = courseId,
            CourseCode = "PPL-101",
            CourseName = "Private Pilot",
            Description = "Desc",
            DurationHours = 100,
            Status = CourseStatus.Active,
            ValidityMonths = 24,
            CourseType = "Pilot",
            VersionNo = 1
        };

        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCourse);

        // Course is locked due to InProgress class
        _mockClassRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Class>
            {
                new() { ClassId = 1, CourseId = courseId, Status = ClassStatus.InProgress, IsDeleted = false }
            });

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject>
            {
                new() { CourseId = courseId, SubjectId = 1, SequenceNo = 1, RequiredHours = 40, RequiredSessions = 10, IsMandatory = true, PassingScore = 75 }
            });

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<CourseResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CourseResponse>>, CancellationToken>((op, ct) => op(ct));

        // Request changes PassingScore on subject from 75 to 85
        var request = new UpdateCourseRequest(
            courseId,
            "PPL-101",
            "Private Pilot",
            "Desc",
            100,
            CourseStatus.Active,
            24,
            "Pilot",
            new List<AddCourseSubjectRequest>
            {
                new() { SubjectId = 1, SequenceNo = 1, RequiredHours = 40, RequiredSessions = 10, IsMandatory = true, PassingScore = 85 }
            }
        );

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.UpdateCourseAsync(courseId, request, updatedByAccountId: 1));

        Assert.Contains("đóng băng bất biến", ex.Message);
    }

    [Fact]
    public async Task UpdateCourseAsync_ShouldSucceed_WhenOnlyTransitioningStatusToArchivedOnLockedCourse()
    {
        int courseId = 10;
        var existingCourse = new Course
        {
            CourseId = courseId,
            CourseCode = "PPL-101",
            CourseName = "Private Pilot",
            Description = "Desc",
            DurationHours = 100,
            Status = CourseStatus.Active,
            ValidityMonths = 24,
            CourseType = "Pilot",
            VersionNo = 1
        };

        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCourse);

        // Course is locked due to Completed class
        _mockClassRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Class>
            {
                new() { ClassId = 1, CourseId = courseId, Status = ClassStatus.Completed, IsDeleted = false }
            });

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject>
            {
                new() { CourseId = courseId, SubjectId = 1, SequenceNo = 1, RequiredHours = 40, RequiredSessions = 10, IsMandatory = true, PassingScore = 75 }
            });

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<CourseResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CourseResponse>>, CancellationToken>((op, ct) => op(ct));

        // Request only changes status from Active to Archived
        var request = new UpdateCourseRequest(
            courseId,
            "PPL-101",
            "Private Pilot",
            "Desc",
            100,
            CourseStatus.Archived,
            24,
            "Pilot",
            new List<AddCourseSubjectRequest>
            {
                new() { SubjectId = 1, SequenceNo = 1, RequiredHours = 40, RequiredSessions = 10, IsMandatory = true, PassingScore = 75 }
            }
        );

        var result = await _service.UpdateCourseAsync(courseId, request, updatedByAccountId: 1);

        Assert.Equal(CourseStatus.Archived, result.Status);
        Assert.Equal(CourseStatus.Archived, existingCourse.Status);
        _mockCourseRepo.Verify(r => r.Update(existingCourse), Times.Once);
    }

    [Fact]
    public async Task CloneCourseVersionAsync_ShouldCopySubjectVersion_ToNewCourseSubjects()
    {
        int courseId = 10;
        var originalCourse = new Course
        {
            CourseId = courseId,
            CourseCode = "PPL-101",
            CourseName = "Private Pilot",
            Description = "Desc",
            DurationHours = 100,
            Status = CourseStatus.Active,
            ValidityMonths = 24,
            CourseType = "Pilot",
            VersionNo = 1
        };

        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalCourse);

        _mockCourseRepo.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Course> { originalCourse });

        var originalSubjects = new List<CourseSubject>
        {
            new() { CourseId = courseId, SubjectId = 1, SequenceNo = 1, RequiredHours = 40, RequiredSessions = 10, IsMandatory = true, PassingScore = 75, SubjectVersion = "2026.1" },
            new() { CourseId = courseId, SubjectId = 2, SequenceNo = 2, RequiredHours = 20, RequiredSessions = 5, IsMandatory = false, PassingScore = 80, SubjectVersion = "2026.2" }
        };

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalSubjects);

        var mockAssessmentRepo = new Mock<IGenericRepository<Assessment>>();
        mockAssessmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Assessment>());
        _mockUow.Setup(u => u.AssessmentRepository).Returns(mockAssessmentRepo.Object);

        var mockChecklistRepo = new Mock<IGenericRepository<PracticalChecklist>>();
        mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklist>());
        _mockUow.Setup(u => u.PracticalChecklistRepository).Returns(mockChecklistRepo.Object);

        var mockReqRepo = new Mock<IGenericRepository<CompletionRequirement>>();
        mockReqRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CompletionRequirement>());
        _mockUow.Setup(u => u.CompletionRequirementRepository).Returns(mockReqRepo.Object);

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<CourseResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CourseResponse>>, CancellationToken>((op, ct) => op(ct));

        var addedCourseSubjects = new List<CourseSubject>();
        _mockCourseSubjectRepo.Setup(r => r.AddAsync(It.IsAny<CourseSubject>(), It.IsAny<CancellationToken>()))
            .Callback<CourseSubject, CancellationToken>((cs, _) => addedCourseSubjects.Add(cs))
            .Returns(Task.CompletedTask);

        var response = await _service.CloneCourseVersionAsync(courseId, createdByAccountId: 99);

        Assert.NotNull(response);
        Assert.Equal(2, response.VersionNo);
        Assert.Equal(CourseStatus.Draft, response.Status);
        Assert.Equal(2, addedCourseSubjects.Count);
        Assert.Equal("2026.1", addedCourseSubjects[0].SubjectVersion);
        Assert.Equal("2026.2", addedCourseSubjects[1].SubjectVersion);
        Assert.Equal("2026.1", response.Subjects![0].SubjectVersion);
        Assert.Equal("2026.2", response.Subjects![1].SubjectVersion);
    }
}
