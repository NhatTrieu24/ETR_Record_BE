using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.DTOs.Evidence.Requests;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;
using Xunit;

namespace ETR.Application.Tests.Services;

public class EtrDossierAuthorizationTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IETRCourseRecordRepository> _mockEtrRepo;
    private readonly Mock<IGenericRepository<CourseEnrollment>> _mockEnrollmentRepo;
    private readonly Mock<IGenericRepository<Class>> _mockClassRepo;
    private readonly Mock<IGenericRepository<CourseSubject>> _mockCourseSubjectRepo;
    private readonly Mock<IGenericRepository<Subject>> _mockSubjectRepo;
    private readonly Mock<IGenericRepository<SubjectResult>> _mockSubjectResultRepo;
    private readonly Mock<IGenericRepository<SubjectSignoff>> _mockSignoffRepo;
    private readonly Mock<IGenericRepository<EvidenceFile>> _mockEvidenceRepo;
    private readonly Mock<IGenericRepository<Attachment>> _mockAttachmentRepo;
    private readonly Mock<IGenericRepository<CompletionRequirement>> _mockRequirementRepo;
    private readonly Mock<IGenericRepository<PracticalChecklist>> _mockChecklistRepo;
    private readonly Mock<IGenericRepository<PracticalChecklistResult>> _mockChecklistResultRepo;
    private readonly Mock<IGenericRepository<ApprovalRequest>> _mockApprovalRepo;
    private readonly Mock<IGenericRepository<ApprovalHistory>> _mockApprovalHistoryRepo;
    private readonly Mock<IAuditLogRepository> _mockAuditRepo;
    private readonly Mock<IGenericRepository<AttendanceRecord>> _mockAttendanceRepo;
    private readonly Mock<IGenericRepository<Session>> _mockSessionRepo;
    private readonly Mock<IGenericRepository<UserProfile>> _mockProfileRepo;
    private readonly Mock<IGenericRepository<Account>> _mockAccountRepo;
    private readonly Mock<IGenericRepository<Course>> _mockCourseRepo;
    private readonly Mock<IGenericRepository<ClassSubject>> _mockClassSubjectRepo;
    private readonly Mock<IAssessmentResultService> _mockAssessmentResultService;
    private readonly Mock<IGenericRepository<AssessmentResult>> _mockAssessmentResultRepo;
    private readonly Mock<IGenericRepository<Assessment>> _mockAssessmentRepo;

    private readonly EtrService _etrService;
    private readonly EvidenceService _evidenceService;

    public EtrDossierAuthorizationTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockEtrRepo = new Mock<IETRCourseRecordRepository>();
        _mockEnrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        _mockClassRepo = new Mock<IGenericRepository<Class>>();
        _mockCourseSubjectRepo = new Mock<IGenericRepository<CourseSubject>>();
        _mockSubjectRepo = new Mock<IGenericRepository<Subject>>();
        _mockSubjectResultRepo = new Mock<IGenericRepository<SubjectResult>>();
        _mockSignoffRepo = new Mock<IGenericRepository<SubjectSignoff>>();
        _mockEvidenceRepo = new Mock<IGenericRepository<EvidenceFile>>();
        _mockAttachmentRepo = new Mock<IGenericRepository<Attachment>>();
        _mockRequirementRepo = new Mock<IGenericRepository<CompletionRequirement>>();
        _mockChecklistRepo = new Mock<IGenericRepository<PracticalChecklist>>();
        _mockChecklistResultRepo = new Mock<IGenericRepository<PracticalChecklistResult>>();
        _mockApprovalRepo = new Mock<IGenericRepository<ApprovalRequest>>();
        _mockApprovalHistoryRepo = new Mock<IGenericRepository<ApprovalHistory>>();
        _mockAuditRepo = new Mock<IAuditLogRepository>();
        _mockAttendanceRepo = new Mock<IGenericRepository<AttendanceRecord>>();
        _mockSessionRepo = new Mock<IGenericRepository<Session>>();
        _mockProfileRepo = new Mock<IGenericRepository<UserProfile>>();
        _mockAccountRepo = new Mock<IGenericRepository<Account>>();
        _mockCourseRepo = new Mock<IGenericRepository<Course>>();
        _mockClassSubjectRepo = new Mock<IGenericRepository<ClassSubject>>();
        _mockAssessmentResultService = new Mock<IAssessmentResultService>();
        _mockAssessmentResultRepo = new Mock<IGenericRepository<AssessmentResult>>();
        _mockAssessmentRepo = new Mock<IGenericRepository<Assessment>>();

        _mockUow.Setup(u => u.ETRCourseRecordRepository).Returns(_mockEtrRepo.Object);
        _mockUow.Setup(u => u.CourseEnrollmentRepository).Returns(_mockEnrollmentRepo.Object);
        _mockUow.Setup(u => u.ClassRepository).Returns(_mockClassRepo.Object);
        _mockUow.Setup(u => u.CourseSubjectRepository).Returns(_mockCourseSubjectRepo.Object);
        _mockUow.Setup(u => u.SubjectRepository).Returns(_mockSubjectRepo.Object);
        _mockUow.Setup(u => u.SubjectResultRepository).Returns(_mockSubjectResultRepo.Object);
        _mockUow.Setup(u => u.SubjectSignoffRepository).Returns(_mockSignoffRepo.Object);
        _mockUow.Setup(u => u.EvidenceFileRepository).Returns(_mockEvidenceRepo.Object);
        _mockUow.Setup(u => u.AttachmentRepository).Returns(_mockAttachmentRepo.Object);
        _mockUow.Setup(u => u.CompletionRequirementRepository).Returns(_mockRequirementRepo.Object);
        _mockUow.Setup(u => u.PracticalChecklistRepository).Returns(_mockChecklistRepo.Object);
        _mockUow.Setup(u => u.PracticalChecklistResultRepository).Returns(_mockChecklistResultRepo.Object);
        _mockUow.Setup(u => u.ApprovalRequestRepository).Returns(_mockApprovalRepo.Object);
        _mockUow.Setup(u => u.ApprovalHistoryRepository).Returns(_mockApprovalHistoryRepo.Object);
        _mockUow.Setup(u => u.AuditLogRepository).Returns(_mockAuditRepo.Object);
        _mockUow.Setup(u => u.AttendanceRecordRepository).Returns(_mockAttendanceRepo.Object);
        _mockUow.Setup(u => u.SessionRepository).Returns(_mockSessionRepo.Object);
        _mockUow.Setup(u => u.UserProfileRepository).Returns(_mockProfileRepo.Object);
        _mockUow.Setup(u => u.AccountRepository).Returns(_mockAccountRepo.Object);
        _mockUow.Setup(u => u.CourseRepository).Returns(_mockCourseRepo.Object);
        _mockUow.Setup(u => u.ClassSubjectRepository).Returns(_mockClassSubjectRepo.Object);
        _mockUow.Setup(u => u.AssessmentResultRepository).Returns(_mockAssessmentResultRepo.Object);
        _mockUow.Setup(u => u.AssessmentRepository).Returns(_mockAssessmentRepo.Object);

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<EtrRecordResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<EtrRecordResponse>>, CancellationToken>((op, ct) => op(ct));

        _mockProfileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserProfile>());
        _mockAccountRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Account>());
        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CourseSubject>());
        _mockSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Subject>());
        _mockAttendanceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AttendanceRecord>());
        _mockSessionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Session>());
        _mockChecklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklistResult>());
        _mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklist>());
        _mockAssessmentResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AssessmentResult>());
        _mockAssessmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Assessment>());
        _mockApprovalHistoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApprovalHistory>());
        _mockApprovalRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ApprovalRequest>());
        _mockAttachmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Attachment>());
        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<EvidenceFile>());
        _mockSignoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SubjectSignoff>());
        _mockRequirementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CompletionRequirement>());

        _etrService = new EtrService(_mockUow.Object, _mockCurrentUserService.Object);
        _evidenceService = new EvidenceService(_mockUow.Object, _mockAssessmentResultService.Object);
    }

    [Fact]
    public async Task SubmitEtrAsync_InstructorNotAssignedToClass_ThrowsForbiddenAccessException()
    {
        // Arrange
        int etrId = 1;
        int instructorId = 50;
        int enrollmentId = 10;
        int otherClassId = 200;

        _mockCurrentUserService.Setup(c => c.RoleName).Returns("Instructor");
        _mockCurrentUserService.Setup(c => c.AccountId).Returns(instructorId);

        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.Draft,
            IsLocked = false
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(etr);

        // Instructor is assigned only to class 999, but this student's enrollment is in class 200
        var classSubjects = new List<ClassSubject>
        {
            new() { ClassId = 999, InstructorAccountId = instructorId }
        };
        _mockClassSubjectRepo.Setup(r => r.GetQueryable()).Returns(classSubjects.AsQueryable());

        var enrollments = new List<CourseEnrollment>
        {
            new() { EnrollmentId = enrollmentId, ClassId = otherClassId, AccountId = 101 }
        };
        _mockEnrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _etrService.SubmitEtrAsync(etrId, instructorId));

        Assert.Contains("Bạn không được phân công giảng dạy", ex.Message);
    }

    [Fact]
    public async Task SubmitEtrAsync_InstructorAssignedToClass_PassesScopeCheck()
    {
        // Arrange
        int etrId = 1;
        int instructorId = 50;
        int enrollmentId = 10;
        int myClassId = 100;
        int courseId = 300;

        _mockCurrentUserService.Setup(c => c.RoleName).Returns("Instructor");
        _mockCurrentUserService.Setup(c => c.AccountId).Returns(instructorId);

        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.Draft,
            IsLocked = false,
            SubjectResults = new List<SubjectResult>
            {
                new()
                {
                    SubjectResultId = 1,
                    SubjectId = 10,
                    Status = SubjectResultStatus.Passed,
                    AttendanceRate = 100,
                    IsMandatorySnapshot = true
                }
            }
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(etr);

        var classSubjects = new List<ClassSubject>
        {
            new() { ClassId = myClassId, InstructorAccountId = instructorId }
        };
        _mockClassSubjectRepo.Setup(r => r.GetQueryable()).Returns(classSubjects.AsQueryable());

        var enrollment = new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = myClassId, AccountId = 101 };
        _mockEnrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseEnrollment> { enrollment });
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        _mockClassRepo.Setup(r => r.GetByIdAsync(myClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = myClassId, CourseId = courseId });

        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EvidenceFile>());

        _mockSignoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectSignoff>
            {
                new() { SubjectResultId = 1, IsDeleted = false }
            });

        _mockRequirementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompletionRequirement>());

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject>());

        // Act
        var result = await _etrService.SubmitEtrAsync(etrId, instructorId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EtrStatus.Submitted, result.Status);
    }

    [Fact]
    public async Task GetEtrDossierAsync_TrainingManager_ReturnsMetadataOnlyForEvidenceAndCredentials()
    {
        // Arrange
        int etrId = 1;
        int enrollmentId = 10;
        int studentAccountId = 101;
        int tmAccountId = 88;

        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.Verified,
            CourseVersionNo = 1,
            SubjectResults = new List<SubjectResult>
            {
                new() { SubjectResultId = 1, SubjectId = 10, Status = SubjectResultStatus.Passed }
            }
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(etr);

        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = 100, AccountId = studentAccountId });

        _mockProfileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile>
            {
                new()
                {
                    AccountId = studentAccountId,
                    FullName = "Nguyen Van A",
                    LicenseNumber = "VN-12345678",
                    MedicalClass = "Class 1",
                    IsCredentialsVerified = true
                }
            });

        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EvidenceFile>
            {
                new() { EvidenceFileId = 50, SubjectResultId = 1, VerificationStatus = "Verified" }
            });

        _mockAttachmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Attachment>
            {
                new()
                {
                    OwnerType = nameof(EvidenceFile),
                    OwnerId = 50,
                    FileName = "evidence.pdf",
                    Url = "https://cloudinary.com/secret/evidence.pdf"
                },
                new()
                {
                    OwnerType = nameof(UserProfile),
                    OwnerId = studentAccountId,
                    FileName = "license.pdf",
                    Url = "https://cloudinary.com/secret/license.pdf"
                }
            });

        // Act
        var dossier = await _etrService.GetEtrDossierAsync(etrId, tmAccountId, "TrainingManager");

        // Assert
        // 1. Evidence FileUrl must be empty (Metadata-only)
        var evidenceItem = dossier.Subjects.First().EvidenceFiles.First();
        Assert.Equal("evidence.pdf", evidenceItem.FileName);
        Assert.Equal(string.Empty, evidenceItem.FileUrl);

        // 2. License number masked
        Assert.NotNull(dossier.Credentials);
        Assert.Equal("***5678", dossier.Credentials.LicenseNumber);

        // 3. Credential attachments list empty for TM
        Assert.Empty(dossier.Credentials.Attachments);

        // 4. AllowedActions includes ExportPdf and Complete
        Assert.Contains("ExportPdf", dossier.AllowedActions);
        Assert.Contains("Complete", dossier.AllowedActions);
    }

    [Fact]
    public async Task GetEtrDossierAsync_StudentSelf_ReturnsEvidenceFileUrlAndCredentialAttachments()
    {
        // Arrange
        int etrId = 1;
        int enrollmentId = 10;
        int studentAccountId = 101;

        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.Completed,
            SubjectResults = new List<SubjectResult>
            {
                new() { SubjectResultId = 1, SubjectId = 10, Status = SubjectResultStatus.Passed }
            }
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(etr);

        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = 100, AccountId = studentAccountId });

        _mockProfileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile>
            {
                new()
                {
                    AccountId = studentAccountId,
                    FullName = "Nguyen Van A",
                    LicenseNumber = "VN-12345678",
                    MedicalClass = "Class 1",
                    IsCredentialsVerified = true
                }
            });

        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EvidenceFile>
            {
                new() { EvidenceFileId = 50, SubjectResultId = 1, VerificationStatus = "Verified" }
            });

        _mockAttachmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Attachment>
            {
                new()
                {
                    OwnerType = nameof(EvidenceFile),
                    OwnerId = 50,
                    FileName = "evidence.pdf",
                    Url = "https://cloudinary.com/secret/evidence.pdf"
                },
                new()
                {
                    OwnerType = nameof(UserProfile),
                    OwnerId = studentAccountId,
                    FileName = "license.pdf",
                    Url = "https://cloudinary.com/secret/license.pdf"
                }
            });

        // Act
        var dossier = await _etrService.GetEtrDossierAsync(etrId, studentAccountId, "Student");

        // Assert
        // 1. Evidence FileUrl present for Student self
        var evidenceItem = dossier.Subjects.First().EvidenceFiles.First();
        Assert.Equal("https://cloudinary.com/secret/evidence.pdf", evidenceItem.FileUrl);

        // 2. Full license number for self
        Assert.NotNull(dossier.Credentials);
        Assert.Equal("VN-12345678", dossier.Credentials.LicenseNumber);

        // 3. Credential attachments present for self
        Assert.Single(dossier.Credentials.Attachments);
        Assert.Equal("https://cloudinary.com/secret/license.pdf", dossier.Credentials.Attachments.First().Url);
    }

    [Fact]
    public async Task EvidenceService_StudentAccessOtherStudentEvidence_ThrowsForbiddenException()
    {
        // Arrange
        int evidenceId = 5;
        int currentStudentId = 101;
        int otherStudentId = 202;

        var evidence = new EvidenceFile
        {
            EvidenceFileId = evidenceId,
            AccountId = otherStudentId, // Belongs to someone else!
            VerificationStatus = "Pending"
        };

        _mockEvidenceRepo.Setup(r => r.GetByIdAsync(evidenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evidence);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _evidenceService.GetEvidenceByIdAsync(evidenceId, currentStudentId, "Student"));

        Assert.Contains("Bạn chỉ được phép xem minh chứng của chính mình", ex.Message);
    }

    [Fact]
    public async Task EvidenceService_TrainingManager_ReturnsMetadataOnlyWithEmptyFileUrl()
    {
        // Arrange
        int evidenceId = 5;
        int tmAccountId = 88;

        var evidence = new EvidenceFile
        {
            EvidenceFileId = evidenceId,
            AccountId = 101,
            VerificationStatus = "Verified"
        };

        _mockEvidenceRepo.Setup(r => r.GetByIdAsync(evidenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evidence);

        _mockAttachmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Attachment>
            {
                new()
                {
                    OwnerType = nameof(EvidenceFile),
                    OwnerId = evidenceId,
                    FileName = "flight_log.pdf",
                    Url = "https://cloudinary.com/files/flight_log.pdf"
                }
            });

        // Act
        var result = await _evidenceService.GetEvidenceByIdAsync(evidenceId, tmAccountId, "TrainingManager");

        // Assert
        Assert.Equal("flight_log.pdf", result.FileName);
        Assert.Equal(string.Empty, result.FileUrl); // Must be empty for TM
    }
}
