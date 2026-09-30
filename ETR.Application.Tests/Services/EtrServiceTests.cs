using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;

namespace ETR.Application.Tests.Services;

public class EtrServiceTests
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
    private readonly Mock<IGenericRepository<CompletionRequirement>> _mockRequirementRepo;
    private readonly Mock<IGenericRepository<PracticalChecklist>> _mockChecklistRepo;
    private readonly Mock<IGenericRepository<PracticalChecklistResult>> _mockChecklistResultRepo;
    private readonly Mock<IGenericRepository<ApprovalRequest>> _mockApprovalRepo;
    private readonly Mock<IAuditLogRepository> _mockAuditRepo;
    private readonly EtrService _service;

    public EtrServiceTests()
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
        _mockRequirementRepo = new Mock<IGenericRepository<CompletionRequirement>>();
        _mockChecklistRepo = new Mock<IGenericRepository<PracticalChecklist>>();
        _mockChecklistResultRepo = new Mock<IGenericRepository<PracticalChecklistResult>>();
        _mockApprovalRepo = new Mock<IGenericRepository<ApprovalRequest>>();
        _mockAuditRepo = new Mock<IAuditLogRepository>();

        _mockUow.Setup(u => u.ETRCourseRecordRepository).Returns(_mockEtrRepo.Object);
        _mockUow.Setup(u => u.CourseEnrollmentRepository).Returns(_mockEnrollmentRepo.Object);
        _mockUow.Setup(u => u.ClassRepository).Returns(_mockClassRepo.Object);
        _mockUow.Setup(u => u.CourseSubjectRepository).Returns(_mockCourseSubjectRepo.Object);
        _mockUow.Setup(u => u.SubjectRepository).Returns(_mockSubjectRepo.Object);
        _mockUow.Setup(u => u.SubjectResultRepository).Returns(_mockSubjectResultRepo.Object);
        _mockUow.Setup(u => u.SubjectSignoffRepository).Returns(_mockSignoffRepo.Object);
        _mockUow.Setup(u => u.EvidenceFileRepository).Returns(_mockEvidenceRepo.Object);
        _mockUow.Setup(u => u.CompletionRequirementRepository).Returns(_mockRequirementRepo.Object);
        _mockUow.Setup(u => u.PracticalChecklistRepository).Returns(_mockChecklistRepo.Object);
        _mockUow.Setup(u => u.PracticalChecklistResultRepository).Returns(_mockChecklistResultRepo.Object);
        _mockUow.Setup(u => u.ApprovalRequestRepository).Returns(_mockApprovalRepo.Object);
        _mockUow.Setup(u => u.AuditLogRepository).Returns(_mockAuditRepo.Object);

        _mockUow.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<EtrRecordResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<EtrRecordResponse>>, CancellationToken>((op, ct) => op(ct));

        _service = new EtrService(_mockUow.Object, _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task SubmitEtrAsync_ShouldSucceed_WhenAllSubjectsAreElectivesBasedOnSnapshot()
    {
        int etrId = 1;
        int enrollmentId = 10;
        int classId = 100;
        int courseId = 500;

        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.InProgress,
            IsLocked = false,
            CourseVersionNo = 1,
            SubjectResults = new List<SubjectResult>
            {
                new()
                {
                    SubjectResultId = 101,
                    SubjectId = 1,
                    Status = SubjectResultStatus.Pending,
                    AttendanceRate = 90,
                    IsMandatorySnapshot = false // Elective!
                }
            }
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(etr);

        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = classId });

        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = classId, CourseId = courseId });

        // Live CourseSubject has IsMandatory = true (simulating an admin changing live config later)
        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject>
            {
                new() { CourseId = courseId, SubjectId = 1, IsMandatory = true }
            });

        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EvidenceFile>());

        _mockSignoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectSignoff>
            {
                new() { SubjectResultId = 101, IsDeleted = false }
            });

        _mockRequirementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompletionRequirement>
            {
                new() { CourseId = courseId, RequirementType = "AllAssessmentsPassed", IsMandatory = true, VersionNo = 1 }
            });

        _mockApprovalRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ApprovalRequest>());

        var result = await _service.SubmitEtrAsync(etrId, accountId: 99);

        Assert.NotNull(result);
        Assert.Equal(EtrStatus.Submitted, result.Status);
    }

    [Fact]
    public async Task SubmitEtrAsync_ShouldFallbackToLiveConfig_WhenNoSnapshotsExist()
    {
        int etrId = 1;
        int enrollmentId = 10;
        int classId = 100;
        int courseId = 500;

        // Legacy ETR without snapshot
        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.InProgress,
            IsLocked = false,
            CourseVersionNo = 1,
            SubjectResults = new List<SubjectResult>
            {
                new()
                {
                    SubjectResultId = 101,
                    SubjectId = 1,
                    Status = SubjectResultStatus.Pending,
                    AttendanceRate = 90,
                    IsMandatorySnapshot = null // Legacy!
                }
            }
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(etr);

        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = classId });

        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = classId, CourseId = courseId });

        // Live CourseSubject is mandatory
        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject>
            {
                new() { CourseId = courseId, SubjectId = 1, IsMandatory = true }
            });

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.SubmitEtrAsync(etrId, accountId: 99));

        Assert.Contains("Mandatory subject", ex.Message);
    }

    [Fact]
    public async Task SubmitEtrAsync_ShouldEvaluateChecklistSnapshots_WhenCompletionRequirementEnforcesChecklists()
    {
        int etrId = 1;
        int enrollmentId = 10;
        int classId = 100;
        int courseId = 500;

        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.InProgress,
            IsLocked = false,
            CourseVersionNo = 1,
            SubjectResults = new List<SubjectResult>
            {
                new()
                {
                    SubjectResultId = 101,
                    SubjectId = 1,
                    Status = SubjectResultStatus.Passed,
                    AttendanceRate = 90,
                    IsMandatorySnapshot = true
                }
            }
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(etr);

        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = classId });

        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = classId, CourseId = courseId });

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject>
            {
                new() { CourseId = courseId, SubjectId = 1, IsMandatory = true }
            });

        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EvidenceFile>());

        _mockSignoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectSignoff>
            {
                new() { SubjectResultId = 101, IsDeleted = false }
            });

        _mockRequirementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompletionRequirement>
            {
                new() { CourseId = courseId, RequirementType = "AllChecklistsSignedOff", IsMandatory = true, VersionNo = 1 }
            });

        // Live checklist is required, but learner's snapshot was optional and Pending
        _mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PracticalChecklist>
            {
                new() { PracticalChecklistId = 50, CourseId = courseId, SubjectId = 1, IsRequired = true }
            });

        _mockChecklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PracticalChecklistResult>
            {
                new() { SubjectResultId = 101, PracticalChecklistId = 50, IsMandatorySnapshot = false, ResultStatus = "Pending" }
            });

        _mockApprovalRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ApprovalRequest>());

        var result = await _service.SubmitEtrAsync(etrId, accountId: 99);

        Assert.NotNull(result);
        Assert.Equal(EtrStatus.Submitted, result.Status);
    }

    [Fact]
    public async Task SubmitEtrAsync_ShouldThrow_WhenMandatoryCourseSubjectIsMissingFromLearnerResults()
    {
        int etrId = 1;
        int enrollmentId = 10;
        int classId = 100;
        int courseId = 500;

        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.InProgress,
            IsLocked = false,
            CourseVersionNo = 1,
            SubjectResults = new List<SubjectResult>
            {
                new()
                {
                    SubjectResultId = 101,
                    SubjectId = 1,
                    Status = SubjectResultStatus.Passed,
                    AttendanceRate = 90,
                    IsMandatorySnapshot = null // Legacy record without snapshot
                }
            }
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(etr);
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = classId });
        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = classId, CourseId = courseId });

        // Course requires Subject 1 AND Subject 2 as mandatory
        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject>
            {
                new() { CourseId = courseId, SubjectId = 1, IsMandatory = true },
                new() { CourseId = courseId, SubjectId = 2, IsMandatory = true } // Missing in learner record
            });

        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EvidenceFile>());
        _mockSignoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectSignoff>
            {
                new() { SubjectResultId = 101, IsDeleted = false }
            });

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.SubmitEtrAsync(etrId, accountId: 99));

        Assert.Contains("Mandatory subject", ex.Message);
    }
}
