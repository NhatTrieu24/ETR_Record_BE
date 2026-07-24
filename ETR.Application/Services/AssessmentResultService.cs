using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ETR.Application.Services;

public class AssessmentResultService : IAssessmentResultService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssessmentResultService> _logger;

    public AssessmentResultService(IUnitOfWork unitOfWork, ILogger<AssessmentResultService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<AssessmentResultResponse>> GetAllAssessmentResultsAsync(CancellationToken cancellationToken = default)
    {
        var results = await _unitOfWork.AssessmentResultRepository.GetAllAsync(cancellationToken);
        return results.Select(r => new AssessmentResultResponse(
            r.AssessmentResultId, r.AssessmentId, r.AccountId, r.SubjectResultId, r.SessionId, r.Score, r.ResultStatus, r.GradedByAccountId, r.RecordedAt, r.PublishedAt, r.IsPublished, r.TakenAt, r.Remark));
    }

    public async Task<AssessmentResultResponse> GetAssessmentResultByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _unitOfWork.AssessmentResultRepository.GetByIdAsync(id, cancellationToken);
        if (result == null) throw new KeyNotFoundException("AssessmentResult not found.");
        return new AssessmentResultResponse(
            result.AssessmentResultId, result.AssessmentId, result.AccountId, result.SubjectResultId, result.SessionId, result.Score, result.ResultStatus, result.GradedByAccountId, result.RecordedAt, result.PublishedAt, result.IsPublished, result.TakenAt, result.Remark);
    }

    public async Task<IEnumerable<AssessmentResultResponse>> GetAssessmentResultsByClassStudentAsync(int classStudentId, int accountId, string? roleName, CancellationToken cancellationToken = default)
    {
        var classStudent = await _unitOfWork.ClassStudentRepository.GetByIdAsync(classStudentId, cancellationToken)
            ?? throw new KeyNotFoundException("ClassStudent not found.");

        // Zero-Trust: Students may only view their own assessment results.
        if (roleName == "Student" && classStudent.AccountId != accountId)
        {
            throw new ForbiddenAccessException("You are not authorized to view another student's assessment results.");
        }

        var results = (await _unitOfWork.AssessmentResultRepository.GetAllAsync(cancellationToken))
            .Where(r => r.AccountId == classStudent.AccountId);

        return results.Select(r => new AssessmentResultResponse(
            r.AssessmentResultId, r.AssessmentId, r.AccountId, r.SubjectResultId, r.SessionId, r.Score, r.ResultStatus, r.GradedByAccountId, r.RecordedAt, r.PublishedAt, r.IsPublished, r.TakenAt, r.Remark));
    }

    public async Task<AssessmentResultResponse> RecordAssessmentScoreAsync(CreateAssessmentResultRequest request, int recordedByAccountId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var assessment = await _unitOfWork.AssessmentRepository.GetByIdAsync(request.AssessmentId, ct);
                if (assessment == null) throw new BusinessRuleViolationException("Assessment not found.");

                var subjectResult = await _unitOfWork.SubjectResultRepository.GetByIdAsync(request.SubjectResultId, ct);
                if (subjectResult == null) throw new BusinessRuleViolationException("SubjectResult not found.");

                // Verify request.AccountId is a real learner enrolled in a class for this assessment's course —
                // prevents recording a score against an arbitrary/forged AccountId.
                var learnerClassIds = (await _unitOfWork.ClassStudentRepository.GetAllAsync(ct))
                    .Where(cs => cs.AccountId == request.AccountId)
                    .Select(cs => cs.ClassId)
                    .ToList();
                var isEnrolledInAssessmentCourse = (await _unitOfWork.ClassRepository.GetAllAsync(ct))
                    .Any(c => learnerClassIds.Contains(c.ClassId) && c.CourseId == assessment.CourseId);
                if (!isEnrolledInAssessmentCourse)
                {
                    throw new BusinessRuleViolationException($"Account (ID: {request.AccountId}) is not enrolled in a class for this assessment's course.");
                }

                var allResults = await _unitOfWork.AssessmentResultRepository.GetAllAsync(ct);

                // Latest attempt so far — exact match by (AssessmentId, AccountId, SessionId), or
                // legacy record without SessionId (transitional fallback), highest AttemptNo wins.
                var latestResult = allResults
                    .Where(r => r.AssessmentId == request.AssessmentId && r.AccountId == request.AccountId
                        && (r.SessionId == request.SessionId || (r.SessionId == null && request.SessionId.HasValue)))
                    .OrderByDescending(r => r.AttemptNo)
                    .FirstOrDefault();

                int attemptNo = 1;

                if (latestResult != null)
                {
                    attemptNo = latestResult.AttemptNo + 1;

                    if (attemptNo > BusinessRuleEngine.MaxAssessmentAttempts)
                    {
                        throw new BusinessRuleViolationException($"Cannot retake. Maximum of {BusinessRuleEngine.MaxAssessmentAttempts} attempts already reached for this assessment.");
                    }

                    if (!request.AuthorizedByAccountId.HasValue || request.AuthorizedByAccountId.Value == recordedByAccountId)
                    {
                        throw new BusinessRuleViolationException("A retake must be authorized by an account different from the one recording the score.");
                    }

                    var retakeHistory = new RetakeHistory
                    {
                        SubjectResultId = request.SubjectResultId,
                        RetakeDate = DateTime.UtcNow,
                        Reason = "Retake Assessment",
                        PreviousScore = latestResult.Score,
                        NewScore = request.Score,
                        AuthorizedByAccountId = request.AuthorizedByAccountId.Value,
                        AttemptNo = attemptNo,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = recordedByAccountId
                    };
                    await _unitOfWork.RetakeHistoryRepository.AddAsync(retakeHistory, ct);
                }

                // Mỗi lần chấm (kể cả retake) tạo 1 dòng AssessmentResult mới — giữ nguyên lịch sử
                // điểm các lần thi trước thay vì ghi đè (unique index nay gồm cả AttemptNo).
                var result = new AssessmentResult
                {
                    AssessmentId = request.AssessmentId,
                    AccountId = request.AccountId,
                    SubjectResultId = request.SubjectResultId,
                    SessionId = request.SessionId,
                    Score = request.Score,
                    ResultStatus = request.Score >= assessment.PassingScore ? "Passed" : "Failed",
                    Remark = request.Remark,
                    GradedByAccountId = recordedByAccountId,
                    RecordedAt = DateTime.UtcNow,
                    AttemptNo = attemptNo,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAccountId = recordedByAccountId,
                    IsPublished = false,
                    PublishedAt = null
                };
                await _unitOfWork.AssessmentResultRepository.AddAsync(result, ct);

                await _unitOfWork.SaveAsync(ct);

                // Auto-Calculate Total Score
                await CalculateSubjectResultScoreAsync(request.SubjectResultId, ct);
                await _unitOfWork.SaveAsync(ct);

                await _unitOfWork.CommitTransactionAsync(ct);

                return new AssessmentResultResponse(result.AssessmentResultId, result.AssessmentId, result.AccountId, result.SubjectResultId, result.SessionId, result.Score, result.ResultStatus, result.GradedByAccountId, result.RecordedAt, result.PublishedAt, result.IsPublished, result.TakenAt, result.Remark);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    private async Task CalculateSubjectResultScoreAsync(int subjectResultId, CancellationToken ct)
    {
        var subjectResult = await _unitOfWork.SubjectResultRepository.GetByIdAsync(subjectResultId, ct);
        if (subjectResult == null) return;

        var allAssessmentResults = (await _unitOfWork.AssessmentResultRepository.GetAllAsync(ct))
            .Where(r => r.SubjectResultId == subjectResultId).ToList();
        
        var allAssessments = await _unitOfWork.AssessmentRepository.GetAllAsync(ct);

        decimal totalWeightedScore = 0;
        decimal totalWeight = 0;

        foreach (var result in allAssessmentResults)
        {
            var assessment = allAssessments.FirstOrDefault(a => a.AssessmentId == result.AssessmentId);
            if (assessment != null)
            {
                totalWeightedScore += result.Score * assessment.Weight;
                totalWeight += assessment.Weight;
            }
        }

        if (totalWeight > 0)
        {
            subjectResult.Score = totalWeightedScore / totalWeight;
            _unitOfWork.SubjectResultRepository.Update(subjectResult);
        }
    }

    public async Task<AssessmentResultResponse> UpdateAssessmentResultAsync(int id, UpdateAssessmentResultRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var result = await _unitOfWork.AssessmentResultRepository.GetByIdAsync(id, cancellationToken);
        if (result == null) throw new KeyNotFoundException("AssessmentResult not found.");

        if (result.IsPublished)
        {
            throw new BusinessRuleViolationException("Cannot update an AssessmentResult that is already published.");
        }

        result.Score = request.Score;
        result.Remark = request.Remark;
        
        var assessment = await _unitOfWork.AssessmentRepository.GetByIdAsync(result.AssessmentId, cancellationToken);
        if (assessment != null)
        {
            result.ResultStatus = request.Score >= assessment.PassingScore ? "Passed" : "Failed";
        }

        result.UpdatedAt = DateTime.UtcNow;
        result.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.AssessmentResultRepository.Update(result);
        await _unitOfWork.SaveAsync(cancellationToken);

        await CalculateSubjectResultScoreAsync(result.SubjectResultId, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new AssessmentResultResponse(
            result.AssessmentResultId, result.AssessmentId, result.AccountId, result.SubjectResultId, result.SessionId, result.Score, result.ResultStatus, result.GradedByAccountId, result.RecordedAt, result.PublishedAt, result.IsPublished, result.TakenAt, result.Remark);
    }

    public async Task<AssessmentResultResponse> PublishAssessmentResultAsync(int id, int publishedByAccountId, CancellationToken cancellationToken = default)
    {
        var result = await _unitOfWork.AssessmentResultRepository.GetByIdAsync(id, cancellationToken);
        if (result == null) throw new KeyNotFoundException("AssessmentResult not found.");

        if (result.IsPublished)
        {
            throw new BusinessRuleViolationException("AssessmentResult is already published.");
        }

        result.IsPublished = true;
        result.PublishedAt = DateTime.UtcNow;
        result.UpdatedAt = DateTime.UtcNow;
        result.UpdatedByAccountId = publishedByAccountId;

        _unitOfWork.AssessmentResultRepository.Update(result);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new AssessmentResultResponse(
            result.AssessmentResultId, result.AssessmentId, result.AccountId, result.SubjectResultId, result.SessionId, result.Score, result.ResultStatus, result.GradedByAccountId, result.RecordedAt, result.PublishedAt, result.IsPublished, result.TakenAt, result.Remark);
    }

    public async Task DeleteAssessmentResultAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var result = await _unitOfWork.AssessmentResultRepository.GetByIdAsync(id, cancellationToken);
        if (result == null) throw new KeyNotFoundException("AssessmentResult not found.");

        result.IsDeleted = true;
        result.DeletedAt = DateTime.UtcNow;
        result.UpdatedAt = DateTime.UtcNow;
        result.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.AssessmentResultRepository.Update(result);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    public async Task<SubjectSignoffResponse> SignoffSubjectResultAsync(CreateSubjectSignoffRequest request, int signoffByAccountId, string signoffByRoleName, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var subjectResult = await _unitOfWork.SubjectResultRepository.GetByIdAsync(request.SubjectResultId, ct);
                if (subjectResult == null) throw new BusinessRuleViolationException("SubjectResult not found.");

                var signoff = new SubjectSignoff
                {
                    SubjectResultId = request.SubjectResultId,
                    SignoffByAccountId = signoffByAccountId,
                    Role = signoffByRoleName,
                    Comment = request.Comment,
                    SignoffAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAccountId = signoffByAccountId
                };

                await _unitOfWork.SubjectSignoffRepository.AddAsync(signoff, ct);
                await _unitOfWork.SaveAsync(ct);

                // Evaluate Passing Conditions (Strict Gateway)
                await EvaluateSubjectPassabilityAsync(subjectResult.SubjectResultId, ct);
                
                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new SubjectSignoffResponse(signoff.SubjectSignoffId, signoff.SubjectResultId, signoff.SignoffByAccountId, signoff.Role, signoff.SignoffAt, signoff.Comment);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    private async Task EvaluateSubjectPassabilityAsync(int subjectResultId, CancellationToken ct)
    {
        var subjectResult = await _unitOfWork.SubjectResultRepository.GetByIdAsync(subjectResultId, ct);
        if (subjectResult == null) return;

        var courseSubject = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
            .FirstOrDefault(cs => cs.CourseId == subjectResult.CourseId && cs.SubjectId == subjectResult.SubjectId);
        var passingScore = courseSubject?.PassingScore ?? 50m;

        var isPassable = true;

        // 1. Attendance Threshold
        if ((subjectResult.AttendanceRate ?? 0) < BusinessRuleEngine.MinimumAttendanceThreshold)
        {
            isPassable = false;
        }

        // 2. Practical Checklist — ngưỡng đọc từ CourseSubject.PassingScore, không hard-code
        var checklists = (await _unitOfWork.PracticalChecklistRepository.GetAllAsync(ct))
            .Where(p => p.CourseId == subjectResult.CourseId && p.SubjectId == subjectResult.SubjectId && p.IsRequired).ToList();

        var checklistResults = (await _unitOfWork.PracticalChecklistResultRepository.GetAllAsync(ct))
            .Where(r => r.SubjectResultId == subjectResultId).ToList();

        if (checklists.Any(c => !checklistResults.Any(r => r.PracticalChecklistId == c.PracticalChecklistId && r.Score >= passingScore)))
        {
            isPassable = false; // Mandatory checklist not completed
        }

        // 3. Mandatory Evidence Files (At least ONE EvidenceFile must be linked, and all must be Verified)
        var evidenceFiles = (await _unitOfWork.EvidenceFileRepository.GetAllAsync(ct))
            .Where(e => e.SubjectResultId == subjectResultId).ToList();

        if (evidenceFiles.Count == 0 || evidenceFiles.Any(e => e.VerificationStatus != "Verified"))
        {
            isPassable = false; // No evidence file uploaded, or not all evidence has been Verified yet
        }

        // 4. Score check (using CourseSubject.PassingScore)
        if ((subjectResult.Score ?? 0) < passingScore)
        {
            isPassable = false; // Score too low
        }

        subjectResult.Status = isPassable ? "Passed" : "Failed";
        subjectResult.EvaluatedAt = DateTime.UtcNow;
        _unitOfWork.SubjectResultRepository.Update(subjectResult);
    }
}
