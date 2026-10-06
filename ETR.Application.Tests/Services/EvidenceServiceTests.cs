using ETR.Application.Compliance;
using ETR.Application.DTOs.Evidence.Requests;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;
using Xunit;

namespace ETR.Application.Tests.Services;

public class EvidenceServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IAssessmentResultService> _mockAssessmentResultService;
    private readonly Mock<IGenericRepository<EvidenceFile>> _mockEvidenceRepo;
    private readonly Mock<IGenericRepository<SubjectResult>> _mockSubjectResultRepo;
    private readonly Mock<IETRCourseRecordRepository> _mockEtrRepo;
    private readonly Mock<IGenericRepository<CourseEnrollment>> _mockEnrollmentRepo;
    private readonly Mock<IGenericRepository<Class>> _mockClassRepo;
    private readonly Mock<IGenericRepository<ClassSubject>> _mockClassSubjectRepo;
    private readonly Mock<IGenericRepository<Attachment>> _mockAttachmentRepo;
    private readonly Mock<IAuditLogRepository> _mockAuditRepo;
    private readonly Mock<IGenericRepository<SubjectSignoff>> _mockSignoffRepo;
    private readonly EvidenceService _service;

    public EvidenceServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockAssessmentResultService = new Mock<IAssessmentResultService>();
        _mockEvidenceRepo = new Mock<IGenericRepository<EvidenceFile>>();
        _mockSubjectResultRepo = new Mock<IGenericRepository<SubjectResult>>();
        _mockEtrRepo = new Mock<IETRCourseRecordRepository>();
        _mockEnrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        _mockClassRepo = new Mock<IGenericRepository<Class>>();
        _mockClassSubjectRepo = new Mock<IGenericRepository<ClassSubject>>();
        _mockAttachmentRepo = new Mock<IGenericRepository<Attachment>>();
        _mockAuditRepo = new Mock<IAuditLogRepository>();
        _mockSignoffRepo = new Mock<IGenericRepository<SubjectSignoff>>();

        _mockUow.Setup(u => u.EvidenceFileRepository).Returns(_mockEvidenceRepo.Object);
        _mockUow.Setup(u => u.SubjectResultRepository).Returns(_mockSubjectResultRepo.Object);
        _mockUow.Setup(u => u.ETRCourseRecordRepository).Returns(_mockEtrRepo.Object);
        _mockUow.Setup(u => u.CourseEnrollmentRepository).Returns(_mockEnrollmentRepo.Object);
        _mockUow.Setup(u => u.ClassRepository).Returns(_mockClassRepo.Object);
        _mockUow.Setup(u => u.ClassSubjectRepository).Returns(_mockClassSubjectRepo.Object);
        _mockUow.Setup(u => u.AttachmentRepository).Returns(_mockAttachmentRepo.Object);
        _mockUow.Setup(u => u.AuditLogRepository).Returns(_mockAuditRepo.Object);
        _mockUow.Setup(u => u.SubjectSignoffRepository).Returns(_mockSignoffRepo.Object);

        _service = new EvidenceService(_mockUow.Object, _mockAssessmentResultService.Object);
    }

    private void SetupValidEtrContext(EtrStatus status, bool isLocked)
    {
        int subjectResultId = 10;
        int etrId = 100;
        int enrollmentId = 200;
        int classId = 300;
        int subjectId = 5;
        int instructorId = 1;

        _mockSubjectResultRepo.Setup(r => r.GetByIdAsync(subjectResultId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubjectResult { SubjectResultId = subjectResultId, EtrId = etrId, SubjectId = subjectId });

        _mockEtrRepo.Setup(r => r.GetByIdAsync(etrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ETRCourseRecord { ETRCourseRecordId = etrId, EnrollmentId = enrollmentId, Status = status, IsLocked = isLocked });

        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = classId, AccountId = 50 });

        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = classId });

        var classSubjects = new List<ClassSubject>
        {
            new() { ClassId = classId, SubjectId = subjectId, InstructorAccountId = instructorId }
        };
        _mockClassSubjectRepo.Setup(r => r.GetQueryable()).Returns(classSubjects.AsQueryable());
    }

    private static UploadEvidenceRequest CreateValidRequest() => new()
    {
        SubjectResultId = 10,
        AccountId = 50,
        EvidenceTypeId = 1,
        FileName = "practical_flight_checklist.pdf",
        FileUrl = "https://res.cloudinary.com/test/practical_flight_checklist.pdf",
        MimeType = "application/pdf"
    };

    [Theory]
    [InlineData(EtrStatus.Draft)]
    [InlineData(EtrStatus.ReturnedForCorrection)]
    public async Task UploadEvidenceAsync_EtrIsDraftOrReturnedForCorrection_AllowsUpload(EtrStatus allowedStatus)
    {
        // Arrange
        SetupValidEtrContext(allowedStatus, isLocked: false);
        var request = CreateValidRequest();

        // Act
        var response = await _service.UploadEvidenceAsync(request, uploadedByAccountId: 1, uploadedByRoleName: "Instructor");

        // Assert
        Assert.NotNull(response);
        _mockEvidenceRepo.Verify(r => r.AddAsync(It.IsAny<EvidenceFile>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockAttachmentRepo.Verify(r => r.AddAsync(It.IsAny<Attachment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(EtrStatus.Submitted)]
    [InlineData(EtrStatus.Verified)]
    [InlineData(EtrStatus.Completed)]
    [InlineData(EtrStatus.Approved)]
    public async Task UploadEvidenceAsync_EtrIsSubmittedOrBeyond_ThrowsBusinessRuleViolationException(EtrStatus blockedStatus)
    {
        // Arrange
        SetupValidEtrContext(blockedStatus, isLocked: false);
        var request = CreateValidRequest();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.UploadEvidenceAsync(request, uploadedByAccountId: 1, uploadedByRoleName: "Instructor"));

        Assert.Contains(blockedStatus.ToString(), ex.Message);
        Assert.Contains("Draft", ex.Message);
        Assert.Contains("ReturnedForCorrection", ex.Message);
    }

    [Fact]
    public async Task UploadEvidenceAsync_EtrIsLocked_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        SetupValidEtrContext(EtrStatus.Draft, isLocked: true);
        var request = CreateValidRequest();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.UploadEvidenceAsync(request, uploadedByAccountId: 1, uploadedByRoleName: "Instructor"));

        Assert.Contains("khóa", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(EtrStatus.Submitted)]
    [InlineData(EtrStatus.Verified)]
    [InlineData(EtrStatus.Completed)]
    public async Task DeleteEvidenceAsync_EtrIsSubmittedOrBeyond_ThrowsBusinessRuleViolationException(EtrStatus blockedStatus)
    {
        // Arrange
        int evidenceId = 99;
        int subjectResultId = 10;
        int etrId = 100;

        _mockEvidenceRepo.Setup(r => r.GetByIdAsync(evidenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvidenceFile { EvidenceFileId = evidenceId, SubjectResultId = subjectResultId, VerificationStatus = "Pending" });

        _mockSignoffRepo.Setup(r => r.GetQueryable()).Returns(new List<SubjectSignoff>().AsQueryable());

        _mockSubjectResultRepo.Setup(r => r.GetByIdAsync(subjectResultId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubjectResult { SubjectResultId = subjectResultId, EtrId = etrId });

        _mockEtrRepo.Setup(r => r.GetByIdAsync(etrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ETRCourseRecord { ETRCourseRecordId = etrId, Status = blockedStatus, IsLocked = false });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.DeleteEvidenceAsync(evidenceId, deletedByAccountId: 1));

        Assert.Contains(blockedStatus.ToString(), ex.Message);
    }

    [Fact]
    public async Task DeleteEvidenceAsync_EtrIsLocked_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        int evidenceId = 99;
        int subjectResultId = 10;
        int etrId = 100;

        _mockEvidenceRepo.Setup(r => r.GetByIdAsync(evidenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvidenceFile { EvidenceFileId = evidenceId, SubjectResultId = subjectResultId, VerificationStatus = "Pending" });

        _mockSignoffRepo.Setup(r => r.GetQueryable()).Returns(new List<SubjectSignoff>().AsQueryable());

        _mockSubjectResultRepo.Setup(r => r.GetByIdAsync(subjectResultId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubjectResult { SubjectResultId = subjectResultId, EtrId = etrId });

        _mockEtrRepo.Setup(r => r.GetByIdAsync(etrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ETRCourseRecord { ETRCourseRecordId = etrId, Status = EtrStatus.Draft, IsLocked = true });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.DeleteEvidenceAsync(evidenceId, deletedByAccountId: 1));

        Assert.Contains("khóa", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
