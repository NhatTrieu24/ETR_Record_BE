using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace ETR.Application.Tests.Services;

public class AssessmentResultServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ILogger<AssessmentResultService>> _mockLogger;
    private readonly Mock<IGenericRepository<SubjectResult>> _mockSubjectResultRepo;
    private readonly Mock<IGenericRepository<SubjectSignoff>> _mockSignoffRepo;
    private readonly Mock<IGenericRepository<Assessment>> _mockAssessmentRepo;
    private readonly Mock<IGenericRepository<AssessmentResult>> _mockAssessmentResultRepo;
    private readonly Mock<IGenericRepository<PracticalChecklist>> _mockChecklistRepo;
    private readonly Mock<IGenericRepository<PracticalChecklistResult>> _mockChecklistResultRepo;
    private readonly Mock<IGenericRepository<EvidenceFile>> _mockEvidenceRepo;
    private readonly Mock<IGenericRepository<CourseSubject>> _mockCourseSubjectRepo;
    private readonly Mock<IETRCourseRecordRepository> _mockEtrRepo;
    private readonly Mock<IGenericRepository<CourseEnrollment>> _mockEnrollmentRepo;
    private readonly Mock<IGenericRepository<Class>> _mockClassRepo;
    private readonly Mock<IGenericRepository<ClassSubject>> _mockClassSubjectRepo;
    private readonly Mock<IAuditLogRepository> _mockAuditRepo;
    private readonly AssessmentResultService _service;

    public AssessmentResultServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<AssessmentResultService>>();
        _mockSubjectResultRepo = new Mock<IGenericRepository<SubjectResult>>();
        _mockSignoffRepo = new Mock<IGenericRepository<SubjectSignoff>>();
        _mockAssessmentRepo = new Mock<IGenericRepository<Assessment>>();
        _mockAssessmentResultRepo = new Mock<IGenericRepository<AssessmentResult>>();
        _mockChecklistRepo = new Mock<IGenericRepository<PracticalChecklist>>();
        _mockChecklistResultRepo = new Mock<IGenericRepository<PracticalChecklistResult>>();
        _mockEvidenceRepo = new Mock<IGenericRepository<EvidenceFile>>();
        _mockCourseSubjectRepo = new Mock<IGenericRepository<CourseSubject>>();
        _mockEtrRepo = new Mock<IETRCourseRecordRepository>();
        _mockEnrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        _mockClassRepo = new Mock<IGenericRepository<Class>>();
        _mockClassSubjectRepo = new Mock<IGenericRepository<ClassSubject>>();
        _mockAuditRepo = new Mock<IAuditLogRepository>();

        _mockUow.Setup(u => u.SubjectResultRepository).Returns(_mockSubjectResultRepo.Object);
        _mockUow.Setup(u => u.SubjectSignoffRepository).Returns(_mockSignoffRepo.Object);
        _mockUow.Setup(u => u.AssessmentRepository).Returns(_mockAssessmentRepo.Object);
        _mockUow.Setup(u => u.AssessmentResultRepository).Returns(_mockAssessmentResultRepo.Object);
        _mockUow.Setup(u => u.PracticalChecklistRepository).Returns(_mockChecklistRepo.Object);
        _mockUow.Setup(u => u.PracticalChecklistResultRepository).Returns(_mockChecklistResultRepo.Object);
        _mockUow.Setup(u => u.EvidenceFileRepository).Returns(_mockEvidenceRepo.Object);
        _mockUow.Setup(u => u.CourseSubjectRepository).Returns(_mockCourseSubjectRepo.Object);
        _mockUow.Setup(u => u.ETRCourseRecordRepository).Returns(_mockEtrRepo.Object);
        _mockUow.Setup(u => u.CourseEnrollmentRepository).Returns(_mockEnrollmentRepo.Object);
        _mockUow.Setup(u => u.ClassRepository).Returns(_mockClassRepo.Object);
        _mockUow.Setup(u => u.ClassSubjectRepository).Returns(_mockClassSubjectRepo.Object);
        _mockUow.Setup(u => u.AuditLogRepository).Returns(_mockAuditRepo.Object);

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<SubjectSignoffResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<SubjectSignoffResponse>>, CancellationToken>((op, ct) => op(ct));

        _service = new AssessmentResultService(_mockUow.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SignoffSubjectResultAsync_ShouldSucceed_WhenOnlyOptionalAssessmentsArePending()
    {
        int subjectResultId = 10;
        int courseId = 1;
        int subjectId = 2;
        int etrId = 100;
        int enrollmentId = 50;
        int classId = 20;
        int instructorId = 5;

        var subjectResult = new SubjectResult
        {
            SubjectResultId = subjectResultId,
            CourseId = courseId,
            SubjectId = subjectId,
            EtrId = etrId,
            AttendanceRate = 95,
            Score = 80,
            PassingScoreSnapshot = 70
        };

        _mockSubjectResultRepo.Setup(r => r.GetByIdAsync(subjectResultId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subjectResult);

        _mockEtrRepo.Setup(r => r.GetByIdAsync(etrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ETRCourseRecord { ETRCourseRecordId = etrId, EnrollmentId = enrollmentId });

        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = classId });

        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = classId, CourseId = courseId });

        var classSubjects = new List<ClassSubject>
        {
            new() { ClassId = classId, SubjectId = subjectId, InstructorAccountId = instructorId, IsDeleted = false }
        };
        _mockClassSubjectRepo.Setup(r => r.GetQueryable()).Returns(classSubjects.AsQueryable());

        _mockSignoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectSignoff>());

        // 1 Mandatory Assessment (Passed), 1 Optional Assessment (Pending)
        var studentAssessments = new List<AssessmentResult>
        {
            new() { AssessmentResultId = 1, AssessmentId = 101, SubjectResultId = subjectResultId, IsMandatorySnapshot = true, ResultStatus = "Passed", Score = 80 },
            new() { AssessmentResultId = 2, AssessmentId = 102, SubjectResultId = subjectResultId, IsMandatorySnapshot = false, ResultStatus = "Pending", Score = 0 }
        };

        _mockAssessmentResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(studentAssessments);

        // Optional Checklist (Pending)
        var checklistResults = new List<PracticalChecklistResult>
        {
            new() { PracticalChecklistResultId = 1, PracticalChecklistId = 201, SubjectResultId = subjectResultId, IsMandatorySnapshot = false, ResultStatus = "Pending" }
        };

        _mockChecklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(checklistResults);

        // Evidence file uploaded
        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EvidenceFile>
            {
                new() { EvidenceFileId = 1, SubjectResultId = subjectResultId, VerificationStatus = "Verified", IsDeleted = false }
            });

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject>());

        var request = new CreateSubjectSignoffRequest(subjectResultId, "Signed off with optional pending");
        var response = await _service.SignoffSubjectResultAsync(request, signoffByAccountId: instructorId, signoffByRoleName: "Instructor");

        Assert.NotNull(response);
        Assert.Equal(subjectResultId, response.SubjectResultId);
        Assert.Equal("Instructor", response.Role);
        Assert.Equal(SubjectResultStatus.Passed, subjectResult.Status);
    }

    [Fact]
    public async Task EvaluateSubjectPassabilityAsync_ShouldPass_WhenAllMandatoryChecklistsPassAndEvidenceVerified()
    {
        int subjectResultId = 10;
        int courseId = 1;
        int subjectId = 2;

        var subjectResult = new SubjectResult
        {
            SubjectResultId = subjectResultId,
            CourseId = courseId,
            SubjectId = subjectId,
            AttendanceRate = 90,
            Score = 85,
            PassingScoreSnapshot = 70,
            Status = SubjectResultStatus.Pending
        };

        _mockSubjectResultRepo.Setup(r => r.GetByIdAsync(subjectResultId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subjectResult);

        // Mandatory checklist passed, optional checklist pending
        var checklistResults = new List<PracticalChecklistResult>
        {
            new() { PracticalChecklistResultId = 1, PracticalChecklistId = 201, SubjectResultId = subjectResultId, IsMandatorySnapshot = true, Score = 80, ResultStatus = "Passed" },
            new() { PracticalChecklistResultId = 2, PracticalChecklistId = 202, SubjectResultId = subjectResultId, IsMandatorySnapshot = false, Score = 0, ResultStatus = "Pending" }
        };

        _mockChecklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(checklistResults);

        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EvidenceFile>
            {
                new() { EvidenceFileId = 1, SubjectResultId = subjectResultId, VerificationStatus = "Verified", IsDeleted = false }
            });

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject>());

        await _service.EvaluateSubjectPassabilityAsync(subjectResultId);

        Assert.Equal(SubjectResultStatus.Passed, subjectResult.Status);
        _mockSubjectResultRepo.Verify(r => r.Update(subjectResult), Times.Once);
    }

    [Fact]
    public async Task SignoffSubjectResultAsync_ShouldThrow_WhenMandatoryAssessmentIsFailed()
    {
        int subjectResultId = 10;
        int courseId = 1;
        int subjectId = 2;
        int etrId = 100;
        int enrollmentId = 50;
        int classId = 20;
        int instructorId = 5;

        var subjectResult = new SubjectResult
        {
            SubjectResultId = subjectResultId,
            CourseId = courseId,
            SubjectId = subjectId,
            EtrId = etrId,
            AttendanceRate = 95,
            Score = 80,
            PassingScoreSnapshot = 70
        };

        _mockSubjectResultRepo.Setup(r => r.GetByIdAsync(subjectResultId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subjectResult);
        _mockEtrRepo.Setup(r => r.GetByIdAsync(etrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ETRCourseRecord { ETRCourseRecordId = etrId, EnrollmentId = enrollmentId });
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = classId });
        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = classId, CourseId = courseId });

        var classSubjects = new List<ClassSubject>
        {
            new() { ClassId = classId, SubjectId = subjectId, InstructorAccountId = instructorId, IsDeleted = false }
        };
        _mockClassSubjectRepo.Setup(r => r.GetQueryable()).Returns(classSubjects.AsQueryable());

        _mockSignoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectSignoff>());

        // 1 Mandatory Assessment (Failed)
        var studentAssessments = new List<AssessmentResult>
        {
            new() { AssessmentResultId = 1, AssessmentId = 101, SubjectResultId = subjectResultId, IsMandatorySnapshot = true, ResultStatus = "Failed", Score = 50 }
        };

        _mockAssessmentResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(studentAssessments);
        _mockChecklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PracticalChecklistResult>());
        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EvidenceFile>
            {
                new() { EvidenceFileId = 1, SubjectResultId = subjectResultId, VerificationStatus = "Verified", IsDeleted = false }
            });
        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject>());

        var request = new CreateSubjectSignoffRequest(subjectResultId, "Signed off attempt");
        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.SignoffSubjectResultAsync(request, signoffByAccountId: instructorId, signoffByRoleName: "Instructor"));

        Assert.Contains("Còn bài kiểm tra bắt buộc chưa đạt", ex.Message);
    }

    [Fact]
    public async Task EvaluateSubjectPassabilityAsync_ShouldSetFailed_WhenMandatoryAssessmentIsFailed_EvenIfAverageScorePasses()
    {
        int subjectResultId = 10;
        int courseId = 1;
        int subjectId = 2;

        var subjectResult = new SubjectResult
        {
            SubjectResultId = subjectResultId,
            CourseId = courseId,
            SubjectId = subjectId,
            AttendanceRate = 90,
            Score = 85, // High overall subject score
            PassingScoreSnapshot = 70,
            Status = SubjectResultStatus.Pending
        };

        _mockSubjectResultRepo.Setup(r => r.GetByIdAsync(subjectResultId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subjectResult);

        // Mandatory assessment failed
        var studentAssessments = new List<AssessmentResult>
        {
            new() { AssessmentResultId = 1, AssessmentId = 101, SubjectResultId = subjectResultId, IsMandatorySnapshot = true, PassingScoreSnapshot = 70, Score = 50, ResultStatus = "Failed" }
        };
        _mockAssessmentResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(studentAssessments);

        // Mandatory checklist passed
        var checklistResults = new List<PracticalChecklistResult>
        {
            new() { PracticalChecklistResultId = 1, PracticalChecklistId = 201, SubjectResultId = subjectResultId, IsMandatorySnapshot = true, Score = 80, ResultStatus = "Passed" }
        };
        _mockChecklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(checklistResults);

        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EvidenceFile>
            {
                new() { EvidenceFileId = 1, SubjectResultId = subjectResultId, VerificationStatus = "Verified", IsDeleted = false }
            });

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject>());

        await _service.EvaluateSubjectPassabilityAsync(subjectResultId);

        Assert.Equal(SubjectResultStatus.Failed, subjectResult.Status);
    }
}
