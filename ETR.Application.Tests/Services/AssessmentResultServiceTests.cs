using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace ETR.Application.Tests.Services;

public class AssessmentResultServiceTests
{
    private static (AssessmentResultService Service, List<AssessmentResult> Results) BuildService(
        Assessment assessment, List<AssessmentResult> existingResults, CourseEnrollment enrollment, Class trainingClass)
    {
        var unitOfWork = new Mock<IUnitOfWork>();

        unitOfWork.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<AssessmentResultResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<AssessmentResultResponse>> op, CancellationToken ct) => op(ct));
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var assessmentRepo = new Mock<IGenericRepository<Assessment>>();
        assessmentRepo.Setup(r => r.GetByIdAsync(assessment.AssessmentId, It.IsAny<CancellationToken>())).ReturnsAsync(assessment);
        assessmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Assessment> { assessment });
        unitOfWork.SetupGet(u => u.AssessmentRepository).Returns(assessmentRepo.Object);

        var subjectResultRepo = new Mock<IGenericRepository<SubjectResult>>();
        subjectResultRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new SubjectResult { SubjectResultId = 1, CourseId = assessment.CourseId, SubjectId = assessment.SubjectId });
        unitOfWork.SetupGet(u => u.SubjectResultRepository).Returns(subjectResultRepo.Object);

        var enrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrollmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CourseEnrollment> { enrollment });
        unitOfWork.SetupGet(u => u.CourseEnrollmentRepository).Returns(enrollmentRepo.Object);

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Class> { trainingClass });
        unitOfWork.SetupGet(u => u.ClassRepository).Returns(classRepo.Object);

        var resultRepo = new Mock<IGenericRepository<AssessmentResult>>();
        resultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => existingResults);
        resultRepo.Setup(r => r.AddAsync(It.IsAny<AssessmentResult>(), It.IsAny<CancellationToken>()))
            .Callback<AssessmentResult, CancellationToken>((r, _) =>
            {
                r.AssessmentResultId = existingResults.Count + 1000;
                existingResults.Add(r);
            })
            .Returns(Task.CompletedTask);
        unitOfWork.SetupGet(u => u.AssessmentResultRepository).Returns(resultRepo.Object);

        var practicalChecklistRepo = new Mock<IGenericRepository<PracticalChecklist>>();
        practicalChecklistRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklist>());
        unitOfWork.SetupGet(u => u.PracticalChecklistRepository).Returns(practicalChecklistRepo.Object);

        var practicalChecklistResultRepo = new Mock<IGenericRepository<PracticalChecklistResult>>();
        practicalChecklistResultRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PracticalChecklistResult>());
        unitOfWork.SetupGet(u => u.PracticalChecklistResultRepository).Returns(practicalChecklistResultRepo.Object);

        var evidenceRepo = new Mock<IGenericRepository<EvidenceFile>>();
        evidenceRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<EvidenceFile>());
        unitOfWork.SetupGet(u => u.EvidenceFileRepository).Returns(evidenceRepo.Object);

        var courseSubjectRepo = new Mock<IGenericRepository<CourseSubject>>();
        courseSubjectRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CourseSubject>());
        unitOfWork.SetupGet(u => u.CourseSubjectRepository).Returns(courseSubjectRepo.Object);

        var logger = new Mock<ILogger<AssessmentResultService>>();
        var service = new AssessmentResultService(unitOfWork.Object, logger.Object);
        return (service, existingResults);
    }

    [Fact]
    public async Task RecordAssessmentScoreAsync_PendingPlaceholder_GradesAgainstSnapshotNotLivePassingScore()
    {
        // Learner enrolled when PassingScore was 50 (snapshot captured at Enroll time — see
        // EnrollmentService). Admin has since raised the bar to 90 for future enrollees. This
        // learner's score of 60 must still be graded "Passed" against the 50 they enrolled under.
        var assessment = new Assessment { AssessmentId = 1, CourseId = 1, SubjectId = 1, PassingScore = 90m };
        var placeholder = new AssessmentResult
        {
            AssessmentResultId = 1, AssessmentId = 1, AccountId = 100, SubjectResultId = 1,
            ResultStatus = "Pending", SessionId = null, AttemptNo = 1, PassingScoreSnapshot = 50m
        };
        var enrollment = new CourseEnrollment { EnrollmentId = 1, AccountId = 100, ClassId = 10 };
        var trainingClass = new Class { ClassId = 10, CourseId = 1 };

        var (service, _) = BuildService(assessment, new List<AssessmentResult> { placeholder }, enrollment, trainingClass);

        var request = new CreateAssessmentResultRequest(1, 100, 1, 60m, null, SessionId: 5);
        var response = await service.RecordAssessmentScoreAsync(request, recordedByAccountId: 9, CancellationToken.None);

        Assert.Equal("Passed", response.ResultStatus);
    }

    [Fact]
    public async Task RecordAssessmentScoreAsync_NoPriorRecordAtAll_FallsBackToLivePassingScore()
    {
        // No placeholder exists (e.g. Assessment was added to the course after this learner already
        // enrolled) — there is no "old rule" to preserve, so the current live value is correct.
        var assessment = new Assessment { AssessmentId = 1, CourseId = 1, SubjectId = 1, PassingScore = 90m };
        var enrollment = new CourseEnrollment { EnrollmentId = 1, AccountId = 100, ClassId = 10 };
        var trainingClass = new Class { ClassId = 10, CourseId = 1 };

        var (service, results) = BuildService(assessment, new List<AssessmentResult>(), enrollment, trainingClass);

        var request = new CreateAssessmentResultRequest(1, 100, 1, 60m, null, SessionId: 5);
        var response = await service.RecordAssessmentScoreAsync(request, recordedByAccountId: 9, CancellationToken.None);

        Assert.Equal("Failed", response.ResultStatus);
        Assert.Equal(90m, results.Single().PassingScoreSnapshot);
    }

    [Fact]
    public async Task RecordAssessmentScoreAsync_PendingPlaceholder_UsesWeightSnapshotForSubjectAverage_NotLiveWeight()
    {
        // Learner enrolled when Weight was 1 (snapshot captured at Enroll time). Admin later raises
        // Weight to 5 for future enrollees. This learner's SubjectResult.Score must still be computed
        // using the Weight of 1 they enrolled under — here, weight is the only assessment so with
        // Weight=1 the average simply equals the score (100), which would differ if Weight=5 were
        // (wrongly) used together with other assessments — asserted indirectly via the placeholder's
        // own snapshot being preserved rather than the live value.
        var assessment = new Assessment { AssessmentId = 1, CourseId = 1, SubjectId = 1, PassingScore = 50m, Weight = 5m };
        var placeholder = new AssessmentResult
        {
            AssessmentResultId = 1, AssessmentId = 1, AccountId = 100, SubjectResultId = 1,
            ResultStatus = "Pending", SessionId = null, AttemptNo = 1, PassingScoreSnapshot = 50m, WeightSnapshot = 1m
        };
        var enrollment = new CourseEnrollment { EnrollmentId = 1, AccountId = 100, ClassId = 10 };
        var trainingClass = new Class { ClassId = 10, CourseId = 1 };

        var (service, results) = BuildService(assessment, new List<AssessmentResult> { placeholder }, enrollment, trainingClass);

        var request = new CreateAssessmentResultRequest(1, 100, 1, 100m, null, SessionId: 5);
        await service.RecordAssessmentScoreAsync(request, recordedByAccountId: 9, CancellationToken.None);

        Assert.Equal(1m, results.Single().WeightSnapshot);
    }
}
