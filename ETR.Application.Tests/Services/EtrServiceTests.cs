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
    private readonly Mock<IGenericRepository<AttendanceRecord>> _mockAttendanceRepo;
    private readonly Mock<IGenericRepository<Session>> _mockSessionRepo;
    private readonly Mock<IGenericRepository<UserProfile>> _mockProfileRepo;
    private readonly Mock<IGenericRepository<Course>> _mockCourseRepo;
    private readonly Mock<IGenericRepository<ClassSubject>> _mockClassSubjectRepo;
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
        _mockAttendanceRepo = new Mock<IGenericRepository<AttendanceRecord>>();
        _mockSessionRepo = new Mock<IGenericRepository<Session>>();
        _mockProfileRepo = new Mock<IGenericRepository<UserProfile>>();
        _mockCourseRepo = new Mock<IGenericRepository<Course>>();
        _mockClassSubjectRepo = new Mock<IGenericRepository<ClassSubject>>();

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
        _mockUow.Setup(u => u.AttendanceRecordRepository).Returns(_mockAttendanceRepo.Object);
        _mockUow.Setup(u => u.SessionRepository).Returns(_mockSessionRepo.Object);
        _mockUow.Setup(u => u.UserProfileRepository).Returns(_mockProfileRepo.Object);
        _mockUow.Setup(u => u.CourseRepository).Returns(_mockCourseRepo.Object);
        _mockUow.Setup(u => u.ClassSubjectRepository).Returns(_mockClassSubjectRepo.Object);

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

    [Fact]
    public async Task SubmitEtrAsync_ShouldSucceed_WhenMandatoryChecklistFailedFirst_ThenPassedRetake_UnderAllChecklistsSignedOff()
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

        _mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PracticalChecklist>
            {
                new() { PracticalChecklistId = 50, CourseId = courseId, SubjectId = 1, IsRequired = true }
            });

        // 2 records for the same PracticalChecklistId: Attempt 1 Failed, Attempt 2 Passed (CompletedAt is later)
        _mockChecklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PracticalChecklistResult>
            {
                new() { PracticalChecklistResultId = 1, SubjectResultId = 101, PracticalChecklistId = 50, IsMandatorySnapshot = true, ResultStatus = "Failed", CompletedAt = DateTime.UtcNow.AddDays(-2) },
                new() { PracticalChecklistResultId = 2, SubjectResultId = 101, PracticalChecklistId = 50, IsMandatorySnapshot = true, ResultStatus = "Passed", CompletedAt = DateTime.UtcNow.AddDays(-1) }
            });

        _mockApprovalRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ApprovalRequest>());

        var result = await _service.SubmitEtrAsync(etrId, accountId: 99);

        Assert.NotNull(result);
        Assert.Equal(EtrStatus.Submitted, result.Status);
    }

    [Fact]
    public async Task GetReadinessAssessmentAsync_CourseVersionScoping_UsesRequirementsMatchingEtrVersion()
    {
        int etrId = 1;
        int enrollmentId = 10;
        int classId = 100;
        int courseId = 500;
        int studentAccountId = 77;

        _mockCurrentUserService.Setup(u => u.RoleName).Returns("Admin");
        _mockCurrentUserService.Setup(u => u.AccountId).Returns(99);

        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.InProgress,
            CourseVersionNo = 1, // Enrolled under version 1
            SubjectResults = new List<SubjectResult>
            {
                new() { SubjectResultId = 101, SubjectId = 1, Status = SubjectResultStatus.Passed, AttendanceRate = 95, IsMandatorySnapshot = true }
            }
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>())).ReturnsAsync(etr);
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = classId, AccountId = studentAccountId });
        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = classId, ClassName = "Class PPL-01", CourseId = courseId });
        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = courseId, CourseName = "Private Pilot License", VersionNo = 2 });
        _mockProfileRepo.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile>
            {
                new() { AccountId = studentAccountId, FullName = "Nguyen Van A", IsCredentialsVerified = true }
            });

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = courseId, SubjectId = 1, IsMandatory = true } });
        _mockSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject> { new() { SubjectId = 1, SubjectName = "Air Law", SubjectCode = "ALW" } });
        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<EvidenceFile>());
        _mockSignoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectSignoff> { new() { SubjectResultId = 101, IsDeleted = false } });
        _mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklist>());
        _mockChecklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklistResult>());

        // Version 1 requires 40 flight hours; Version 2 requires 50 flight hours
        _mockRequirementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompletionRequirement>
            {
                new() { RequirementId = 1, CourseId = courseId, RequirementName = "Min Flight Hours", RequirementType = "MinFlightHours", ThresholdValue = 40.0m, VersionNo = 1, IsMandatory = true },
                new() { RequirementId = 2, CourseId = courseId, RequirementName = "Min Flight Hours v2", RequirementType = "MinFlightHours", ThresholdValue = 50.0m, VersionNo = 2, IsMandatory = true }
            });

        // Student has 45 qualified flight hours
        _mockSessionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Session>
            {
                new() { SessionId = 1, TrainingType = TrainingType.Flight, IsConfirmed = true, IsDeleted = false }
            });
        _mockAttendanceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AttendanceRecord>
            {
                new() { AttendanceRecordId = 1, EnrollmentId = enrollmentId, SessionId = 1, Status = AttendanceStatus.Present, FlightHours = 45.0m, SimulatorHours = 0m, IsDeleted = false }
            });

        var readiness = await _service.GetReadinessAssessmentAsync(etrId);

        Assert.NotNull(readiness);
        Assert.Equal(courseId, readiness.CourseId);
        Assert.Equal(1, readiness.CourseVersionNo); // Evaluated against Version 1
        Assert.Equal(45.0m, readiness.TotalFlightHours);
        Assert.Equal(0m, readiness.TotalSimulatorHours);

        var flightCondition = readiness.Conditions.FirstOrDefault(c => c.ConditionCode == "MIN_FLIGHT_HOURS");
        Assert.NotNull(flightCondition);
        Assert.Equal(ReadinessStatus.Met, flightCondition.Status); // 45h >= 40h of Version 1 (Met!)
        Assert.Equal(40.0m, flightCondition.ThresholdValue);
        Assert.Equal(ReadinessStatus.Met, readiness.OverallStatus);
    }

    [Fact]
    public async Task GetReadinessAssessmentAsync_FlightAndSimulatorHours_CalculatedSeparatelyAndExcludesInvalidRecords()
    {
        int etrId = 2;
        int enrollmentId = 20;
        int classId = 200;
        int courseId = 600;
        int studentAccountId = 88;

        _mockCurrentUserService.Setup(u => u.RoleName).Returns("Admin");
        _mockCurrentUserService.Setup(u => u.AccountId).Returns(99);

        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.InProgress,
            CourseVersionNo = 1,
            SubjectResults = new List<SubjectResult>
            {
                new() { SubjectResultId = 201, SubjectId = 1, Status = SubjectResultStatus.Passed, AttendanceRate = 100, IsMandatorySnapshot = true }
            }
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>())).ReturnsAsync(etr);
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = classId, AccountId = studentAccountId });
        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = classId, ClassName = "Class CPL-01", CourseId = courseId });
        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = courseId, CourseName = "Commercial Pilot License", VersionNo = 1 });
        _mockProfileRepo.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile>
            {
                new() { AccountId = studentAccountId, FullName = "Tran Van B", IsCredentialsVerified = true }
            });

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = courseId, SubjectId = 1, IsMandatory = true } });
        _mockSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject> { new() { SubjectId = 1, SubjectName = "Navigation", SubjectCode = "NAV" } });
        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<EvidenceFile>());
        _mockSignoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectSignoff> { new() { SubjectResultId = 201, IsDeleted = false } });
        _mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklist>());
        _mockChecklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklistResult>());
        _mockRequirementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompletionRequirement>
            {
                new() { RequirementId = 1, CourseId = courseId, RequirementName = "Min Flight Hours", RequirementType = "MinFlightHours", ThresholdValue = 100.0m, VersionNo = 1, IsMandatory = true },
                new() { RequirementId = 2, CourseId = courseId, RequirementName = "Min Simulator Hours", RequirementType = "MinSimulatorHours", ThresholdValue = 20.0m, VersionNo = 1, IsMandatory = true }
            });

        _mockSessionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Session>
            {
                new() { SessionId = 1, TrainingType = TrainingType.Flight, IsConfirmed = true, IsDeleted = false },
                new() { SessionId = 2, TrainingType = TrainingType.Simulator, IsConfirmed = true, IsDeleted = false },
                new() { SessionId = 3, TrainingType = TrainingType.Flight, IsConfirmed = false, IsDeleted = false }, // Unconfirmed session, not instructor signed
                new() { SessionId = 4, TrainingType = TrainingType.Flight, IsConfirmed = true, IsDeleted = true } // Deleted session
            });

        _mockAttendanceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AttendanceRecord>
            {
                new() { AttendanceRecordId = 1, EnrollmentId = enrollmentId, SessionId = 1, Status = AttendanceStatus.Present, FlightHours = 80.0m, SimulatorHours = 0m, IsDeleted = false },
                new() { AttendanceRecordId = 2, EnrollmentId = enrollmentId, SessionId = 2, Status = AttendanceStatus.Present, FlightHours = 0m, SimulatorHours = 25.0m, IsDeleted = false },
                new() { AttendanceRecordId = 3, EnrollmentId = enrollmentId, SessionId = 3, Status = AttendanceStatus.Present, FlightHours = 10.0m, SimulatorHours = 0m, IsDeleted = false }, // Unconfirmed/unsigned -> excluded
                new() { AttendanceRecordId = 4, EnrollmentId = enrollmentId, SessionId = 1, Status = AttendanceStatus.Absent, FlightHours = 15.0m, SimulatorHours = 0m, IsDeleted = false }, // Absent -> excluded
                new() { AttendanceRecordId = 5, EnrollmentId = enrollmentId, SessionId = 1, Status = AttendanceStatus.Present, FlightHours = 10.0m, SimulatorHours = 0m, IsDeleted = true }, // Deleted record -> excluded
                new() { AttendanceRecordId = 6, EnrollmentId = enrollmentId, SessionId = 4, Status = AttendanceStatus.Present, FlightHours = 10.0m, SimulatorHours = 0m, IsDeleted = false } // Deleted session -> excluded
            });

        var readiness = await _service.GetReadinessAssessmentAsync(etrId);

        Assert.NotNull(readiness);
        Assert.Equal(80.0m, readiness.TotalFlightHours); // Only valid present confirmed flight hours
        Assert.Equal(25.0m, readiness.TotalSimulatorHours); // Strictly separated, not summed into flight hours

        var flightCond = readiness.Conditions.First(c => c.ConditionCode == "MIN_FLIGHT_HOURS");
        var simCond = readiness.Conditions.First(c => c.ConditionCode == "MIN_SIMULATOR_HOURS");

        Assert.Equal(ReadinessStatus.NotMet, flightCond.Status); // 80 < 100
        Assert.Equal(ReadinessStatus.Met, simCond.Status); // 25 >= 20
        Assert.Equal(ReadinessStatus.NotMet, readiness.OverallStatus);
    }

    [Fact]
    public async Task GetReadinessAssessmentAsync_ZeroHours_ReturnsNoData()
    {
        int etrId = 3;
        int enrollmentId = 30;
        int classId = 300;
        int courseId = 700;
        int studentAccountId = 89;

        _mockCurrentUserService.Setup(u => u.RoleName).Returns("Admin");
        _mockCurrentUserService.Setup(u => u.AccountId).Returns(99);

        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.InProgress,
            CourseVersionNo = 1,
            SubjectResults = new List<SubjectResult>()
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>())).ReturnsAsync(etr);
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = classId, AccountId = studentAccountId });
        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = classId, ClassName = "Class ATP-01", CourseId = courseId });
        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = courseId, CourseName = "ATP Integrated", VersionNo = 1 });
        _mockProfileRepo.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile>
            {
                new() { AccountId = studentAccountId, FullName = "Le Van C", IsCredentialsVerified = false }
            });

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CourseSubject>());
        _mockSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Subject>());
        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<EvidenceFile>());
        _mockSignoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SubjectSignoff>());
        _mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklist>());
        _mockChecklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklistResult>());
        _mockRequirementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompletionRequirement>
            {
                new() { RequirementId = 1, CourseId = courseId, RequirementName = "Min Flight Hours", RequirementType = "MinFlightHours", ThresholdValue = 50.0m, VersionNo = 1, IsMandatory = true }
            });
        _mockSessionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Session>());
        _mockAttendanceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AttendanceRecord>());

        var readiness = await _service.GetReadinessAssessmentAsync(etrId);

        Assert.NotNull(readiness);
        var flightCond = readiness.Conditions.First(c => c.ConditionCode == "MIN_FLIGHT_HOURS");
        Assert.Equal(ReadinessStatus.NoData, flightCond.Status); // 0 hours with threshold 50 returns NoData, does NOT assume 0 is met!
        Assert.Contains("0.00 / 50", flightCond.Explanation);
    }

    [Fact]
    public async Task GetReadinessAssessmentAsync_ExpiredCredentials_AddsWarningsWithoutHardBlocking()
    {
        int etrId = 4;
        int enrollmentId = 40;
        int classId = 400;
        int courseId = 800;
        int studentAccountId = 91;

        _mockCurrentUserService.Setup(u => u.RoleName).Returns("Student");
        _mockCurrentUserService.Setup(u => u.AccountId).Returns(studentAccountId);

        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.InProgress,
            CourseVersionNo = 1,
            SubjectResults = new List<SubjectResult>
            {
                new() { SubjectResultId = 401, SubjectId = 1, Status = SubjectResultStatus.Passed, AttendanceRate = 100, IsMandatorySnapshot = true }
            }
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>())).ReturnsAsync(etr);
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = classId, AccountId = studentAccountId });
        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = classId, ClassName = "Class ME-01", CourseId = courseId });
        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = courseId, CourseName = "Multi-Engine Rating", VersionNo = 1 });

        _mockProfileRepo.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile>
            {
                new()
                {
                    AccountId = studentAccountId,
                    FullName = "Pham Van D",
                    IsCredentialsVerified = false,
                    MedicalClass = "Class 1",
                    MedicalExpiryDate = DateTime.UtcNow.AddDays(-10), // Expired!
                    LicenseType = "CPL",
                    LicenseExpiryDate = DateTime.UtcNow.AddDays(-5) // Expired!
                }
            });

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = courseId, SubjectId = 1, IsMandatory = true } });
        _mockSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject> { new() { SubjectId = 1, SubjectName = "Multi Engine Theory", SubjectCode = "ME" } });
        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<EvidenceFile>());
        _mockSignoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectSignoff> { new() { SubjectResultId = 401, IsDeleted = false } });
        _mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklist>());
        _mockChecklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklistResult>());
        _mockRequirementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CompletionRequirement>());
        _mockSessionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Session>());
        _mockAttendanceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AttendanceRecord>());

        var readiness = await _service.GetReadinessAssessmentAsync(etrId);

        Assert.NotNull(readiness);
        Assert.NotEmpty(readiness.Warnings);
        Assert.Contains(readiness.Warnings, w => w.WarningCode == "CREDENTIALS_UNVERIFIED");
        Assert.Contains(readiness.Warnings, w => w.WarningCode == "MEDICAL_EXPIRED");
        Assert.Contains(readiness.Warnings, w => w.WarningCode == "LICENSE_EXPIRED");

        // ReviewRequired status due to unverified/expired warnings, but NOT throwing exception / hard blocking
        Assert.Equal(ReadinessStatus.ReviewRequired, readiness.OverallStatus);
    }

    [Fact]
    public async Task GetReadinessAssessmentAsync_InstructorViewingStudent_MasksSensitiveCredentialDetails()
    {
        int etrId = 5;
        int enrollmentId = 50;
        int classId = 500;
        int courseId = 900;
        int studentAccountId = 92;
        int instructorAccountId = 44;

        _mockCurrentUserService.Setup(u => u.RoleName).Returns("Instructor");
        _mockCurrentUserService.Setup(u => u.AccountId).Returns(instructorAccountId);

        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.InProgress,
            CourseVersionNo = 1,
            SubjectResults = new List<SubjectResult>
            {
                new() { SubjectResultId = 501, SubjectId = 1, Status = SubjectResultStatus.Passed, AttendanceRate = 100, IsMandatorySnapshot = true }
            }
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>())).ReturnsAsync(etr);
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = classId, AccountId = studentAccountId });
        _mockEnrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseEnrollment> { new() { EnrollmentId = enrollmentId, ClassId = classId, AccountId = studentAccountId } });

        var classSubjects = new List<ClassSubject>
        {
            new() { ClassId = classId, SubjectId = 1, InstructorAccountId = instructorAccountId }
        };
        _mockClassSubjectRepo.Setup(r => r.GetQueryable()).Returns(classSubjects.AsQueryable());

        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = classId, ClassName = "Class IR-01", CourseId = courseId });
        _mockCourseRepo.Setup(r => r.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = courseId, CourseName = "Instrument Rating", VersionNo = 1 });

        _mockProfileRepo.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile>
            {
                new()
                {
                    AccountId = studentAccountId,
                    FullName = "Vu Thi E",
                    IsCredentialsVerified = true,
                    MedicalClass = "Class 1",
                    MedicalExpiryDate = DateTime.UtcNow.AddDays(-15)
                }
            });

        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = courseId, SubjectId = 1, IsMandatory = true } });
        _mockSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Subject> { new() { SubjectId = 1, SubjectName = "Instrument Procedures", SubjectCode = "IFR" } });
        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<EvidenceFile>());
        _mockSignoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectSignoff> { new() { SubjectResultId = 501, IsDeleted = false } });
        _mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklist>());
        _mockChecklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklistResult>());
        _mockRequirementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CompletionRequirement>());
        _mockSessionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Session>());
        _mockAttendanceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AttendanceRecord>());

        var readiness = await _service.GetReadinessAssessmentAsync(etrId);

        Assert.NotNull(readiness);
        var medWarning = readiness.Warnings.FirstOrDefault(w => w.WarningCode == "MEDICAL_EXPIRED");
        Assert.NotNull(medWarning);
        // Sensitive medical class details are masked for Instructor
        Assert.DoesNotContain("Class 1", medWarning.Message);
        Assert.Contains("Giấy chứng nhận sức khỏe của học viên đã hết hạn", medWarning.Message);
    }

    [Fact]
    public async Task GetReadinessAssessmentAsync_StudentViewingAnotherStudent_ThrowsUnauthorizedAccessException()
    {
        int etrId = 6;
        int enrollmentId = 60;
        int studentAccountId = 93;
        int anotherStudentAccountId = 94;

        _mockCurrentUserService.Setup(u => u.RoleName).Returns("Student");
        _mockCurrentUserService.Setup(u => u.AccountId).Returns(anotherStudentAccountId);

        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.InProgress,
            CourseVersionNo = 1
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>())).ReturnsAsync(etr);
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = 600, AccountId = studentAccountId });
        _mockClassRepo.Setup(r => r.GetByIdAsync(600, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = 600, CourseId = 1000 });
        _mockCourseRepo.Setup(r => r.GetByIdAsync(1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Course { CourseId = 1000 });
        _mockProfileRepo.Setup(r => r.GetAllIncludingDeletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetReadinessAssessmentAsync(etrId));
    }

    [Fact]
    public async Task SubmitEtrAsync_MinFlightHoursNotMet_ThrowsBusinessRuleViolationException()
    {
        int etrId = 7;
        int enrollmentId = 70;
        int classId = 700;
        int courseId = 1100;

        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = etrId,
            EnrollmentId = enrollmentId,
            Status = EtrStatus.InProgress,
            CourseVersionNo = 1,
            SubjectResults = new List<SubjectResult>
            {
                new() { SubjectResultId = 701, SubjectId = 1, Status = SubjectResultStatus.Passed, AttendanceRate = 100, IsMandatorySnapshot = true }
            }
        };

        _mockEtrRepo.Setup(r => r.GetWithSubjectResultsAsync(etrId, It.IsAny<CancellationToken>())).ReturnsAsync(etr);
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourseEnrollment { EnrollmentId = enrollmentId, ClassId = classId });
        _mockClassRepo.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class { ClassId = classId, CourseId = courseId });
        _mockCourseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseSubject> { new() { CourseId = courseId, SubjectId = 1, IsMandatory = true } });
        _mockEvidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<EvidenceFile>());
        _mockSignoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectSignoff> { new() { SubjectResultId = 701, IsDeleted = false } });
        _mockChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklist>());
        _mockChecklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklistResult>());

        _mockRequirementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompletionRequirement>
            {
                new() { CourseId = courseId, RequirementType = "MinFlightHours", ThresholdValue = 45.0m, VersionNo = 1, IsMandatory = true }
            });

        // Qualified flight hours = 30.0 (less than 45.0)
        _mockSessionRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Session>
            {
                new() { SessionId = 1, TrainingType = TrainingType.Flight, IsConfirmed = true, IsDeleted = false }
            });
        _mockAttendanceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AttendanceRecord>
            {
                new() { AttendanceRecordId = 1, EnrollmentId = enrollmentId, SessionId = 1, Status = AttendanceStatus.Present, FlightHours = 30.0m, SimulatorHours = 0m, IsDeleted = false }
            });

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _service.SubmitEtrAsync(etrId, accountId: 99));
        Assert.Contains("qualified flight hours", ex.Message);
        Assert.Contains("45", ex.Message);
    }
}
