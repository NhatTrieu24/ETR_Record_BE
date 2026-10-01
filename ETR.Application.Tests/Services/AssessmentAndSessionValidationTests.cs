using ETR.Application.DTOs.Assessment.Requests;
using ETR.Application.DTOs.PracticalChecklist;
using ETR.Application.DTOs.Session;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace ETR.Application.Tests.Services;

public class AssessmentAndSessionValidationTests
{
    private readonly Mock<IUnitOfWork> _mockUow = new();
    private readonly Mock<ICourseService> _mockCourseService = new();
    private readonly Mock<IGenericRepository<Course>> _mockCourseRepo = new();
    private readonly Mock<IGenericRepository<Subject>> _mockSubjectRepo = new();
    private readonly Mock<IGenericRepository<CourseSubject>> _mockCourseSubjectRepo = new();
    private readonly Mock<IGenericRepository<Assessment>> _mockAssessmentRepo = new();
    private readonly Mock<IGenericRepository<PracticalChecklist>> _mockChecklistRepo = new();
    private readonly Mock<IGenericRepository<Session>> _mockSessionRepo = new();
    private readonly Mock<IGenericRepository<Class>> _mockClassRepo = new();
    private readonly Mock<IGenericRepository<ClassSubject>> _mockClassSubjectRepo = new();
    private readonly Mock<IAuditLogRepository> _mockAuditRepo = new();

    public AssessmentAndSessionValidationTests()
    {
        _mockUow.Setup(u => u.CourseRepository).Returns(_mockCourseRepo.Object);
        _mockUow.Setup(u => u.SubjectRepository).Returns(_mockSubjectRepo.Object);
        _mockUow.Setup(u => u.CourseSubjectRepository).Returns(_mockCourseSubjectRepo.Object);
        _mockUow.Setup(u => u.AssessmentRepository).Returns(_mockAssessmentRepo.Object);
        _mockUow.Setup(u => u.PracticalChecklistRepository).Returns(_mockChecklistRepo.Object);
        _mockUow.Setup(u => u.SessionRepository).Returns(_mockSessionRepo.Object);
        _mockUow.Setup(u => u.ClassRepository).Returns(_mockClassRepo.Object);
        _mockUow.Setup(u => u.ClassSubjectRepository).Returns(_mockClassSubjectRepo.Object);
        _mockUow.Setup(u => u.AuditLogRepository).Returns(_mockAuditRepo.Object);
    }

    [Fact]
    public async Task CreateAssessmentAsync_ThrowsValidationException_WhenSubjectDoesNotBelongToCourse()
    {
        var course = new Course { CourseId = 1, CourseCode = "CRS-01" };
        var subject = new Subject { SubjectId = 2, SubjectCode = "SUB-02" };

        _mockCourseRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        _mockSubjectRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(subject);

        // CourseSubject is empty (Subject 2 not in Course 1)
        var courseSubjects = new List<CourseSubject>().AsQueryable();
        _mockCourseSubjectRepo.Setup(r => r.GetQueryable()).Returns(courseSubjects);

        var service = new AssessmentService(_mockUow.Object, _mockCourseService.Object);
        var request = new CreateAssessmentRequest
        {
            CourseId = 1,
            SubjectId = 2,
            ComponentName = "Final Exam",
            AssessmentType = "Theory",
            Weight = 50,
            PassingScore = 75
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAssessmentAsync(request, 10));

        Assert.Contains("không thuộc về Khóa học", ex.Message);
    }

    [Fact]
    public async Task CreatePracticalChecklistAsync_ThrowsValidationException_WhenSubjectDoesNotBelongToCourse()
    {
        var course = new Course { CourseId = 1, CourseCode = "CRS-01" };
        var subject = new Subject { SubjectId = 2, SubjectCode = "SUB-02" };

        _mockCourseRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        _mockSubjectRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(subject);

        var courseSubjects = new List<CourseSubject>().AsQueryable();
        _mockCourseSubjectRepo.Setup(r => r.GetQueryable()).Returns(courseSubjects);

        var service = new PracticalChecklistService(_mockUow.Object, _mockCourseService.Object);
        var request = new CreatePracticalChecklistRequest
        {
            CourseId = 1,
            SubjectId = 2,
            ItemName = "Pre-flight Inspection",
            IsRequired = true
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreatePracticalChecklistAsync(request, 10));

        Assert.Contains("không thuộc về Khóa học", ex.Message);
    }

    [Fact]
    public async Task UpdateSessionAsync_ThrowsValidationException_WhenAssigningTheoryAssessmentToFlightOrSimSession()
    {
        var session = new Session
        {
            SessionId = 100,
            ClassId = 1,
            SubjectId = 5,
            TrainingType = TrainingType.Flight
        };

        var cls = new Class { ClassId = 1, CourseId = 10 };
        var assessment = new Assessment
        {
            AssessmentId = 20,
            CourseId = 10,
            SubjectId = 5,
            ComponentName = "Aviation Law Written Exam",
            AssessmentType = "Theory"
        };

        _mockSessionRepo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _mockClassRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cls);
        _mockAssessmentRepo.Setup(r => r.GetByIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(assessment);

        var service = new SessionService(_mockUow.Object);
        var request = new UpdateSessionRequest
        {
            SessionTitle = "Flight Training Session 1",
            SessionDate = DateTime.UtcNow.AddDays(1),
            TrainingType = TrainingType.Flight,
            AssessmentId = 20,
            IsAssessmentRequired = true
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateSessionAsync(100, request, 1));

        Assert.Contains("Không thể gán bài thi lý thuyết", ex.Message);
    }

    [Fact]
    public async Task UpdateSessionAsync_ThrowsValidationException_WhenAssessmentBelongsToDifferentSubject()
    {
        var session = new Session
        {
            SessionId = 100,
            ClassId = 1,
            SubjectId = 5,
            TrainingType = TrainingType.Theory
        };

        var cls = new Class { ClassId = 1, CourseId = 10 };
        var assessment = new Assessment
        {
            AssessmentId = 20,
            CourseId = 10,
            SubjectId = 99, // Different subject
            ComponentName = "Navigation Exam",
            AssessmentType = "Theory"
        };

        _mockSessionRepo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _mockClassRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cls);
        _mockAssessmentRepo.Setup(r => r.GetByIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(assessment);

        var service = new SessionService(_mockUow.Object);
        var request = new UpdateSessionRequest
        {
            SessionTitle = "Ground Theory Final",
            SessionDate = DateTime.UtcNow.AddDays(1),
            TrainingType = TrainingType.Theory,
            AssessmentId = 20,
            IsAssessmentRequired = true
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateSessionAsync(100, request, 1));

        Assert.Contains("Assessment does not match", ex.Message);
    }
}
