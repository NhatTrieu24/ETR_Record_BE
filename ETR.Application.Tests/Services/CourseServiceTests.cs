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
}
