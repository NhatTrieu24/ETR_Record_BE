using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
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

    public async Task<IEnumerable<AssessmentResultResponse>> GetAssessmentResultsByEnrollmentAsync(int enrollmentId, int accountId, string? roleName, CancellationToken cancellationToken = default)
    {
        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(enrollmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Enrollment not found.");

        // Zero-Trust: Students may only view their own assessment results.
        if (roleName == "Student" && enrollment.AccountId != accountId)
        {
            throw new ForbiddenAccessException("You are not authorized to view another student's assessment results.");
        }

        var results = (await _unitOfWork.AssessmentResultRepository.GetAllAsync(cancellationToken))
            .Where(r => r.AccountId == enrollment.AccountId);

        return results.Select(r => new AssessmentResultResponse(
            r.AssessmentResultId, r.AssessmentId, r.AccountId, r.SubjectResultId, r.SessionId, r.Score, r.ResultStatus, r.GradedByAccountId, r.RecordedAt, r.PublishedAt, r.IsPublished, r.TakenAt, r.Remark));
    }

    public async Task<AssessmentResultResponse> RecordAssessmentScoreAsync(CreateAssessmentResultRequest request, int recordedByAccountId, string? recordedByRoleName, CancellationToken cancellationToken = default)
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
                var learnerClassIds = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(ct))
                    .Where(e => e.AccountId == request.AccountId && !e.IsDeleted)
                    .Select(e => e.ClassId)
                    .ToList();
                var targetClass = (await _unitOfWork.ClassRepository.GetAllAsync(ct))
                    .FirstOrDefault(c => learnerClassIds.Contains(c.ClassId) && c.CourseId == assessment.CourseId);
                if (targetClass == null)
                {
                    throw new BusinessRuleViolationException($"Account (ID: {request.AccountId}) is not enrolled in a class for this assessment's course.");
                }

                // "Sân nhà ai nấy đá" — Instructor can only grade a class they are actually
                // assigned to (see ClassOwnershipValidator).
                var isAssigned = _unitOfWork.ClassSubjectRepository.GetQueryable()
                    .Any(cs => cs.ClassId == targetClass.ClassId && cs.SubjectId == subjectResult.SubjectId && cs.InstructorAccountId == recordedByAccountId);
                ClassOwnershipValidator.EnsureInstructorOwnsSubject(recordedByRoleName, isAssigned);

                var allResults = await _unitOfWork.AssessmentResultRepository.GetAllAsync(ct);

                // Tìm dòng nháp (Pending) được tạo sẵn khi Enroll
                var pendingPlaceholder = allResults.FirstOrDefault(r => 
                    r.AssessmentId == request.AssessmentId && 
                    r.AccountId == request.AccountId && 
                    r.ResultStatus == "Pending" && 
                    r.SessionId == null && 
                    r.AttemptNo == 1);

                AssessmentResult? result;

                if (pendingPlaceholder != null)
                {
                    // Grade against the snapshot taken at Enroll time (see EnrollmentService), NOT
                    // whatever Assessment.PassingScore currently is — a threshold change made after
                    // this learner enrolled must not change how their own attempt is graded. Records
                    // created before this field existed (snapshot null) fall back to the live value.
                    var passingScore = pendingPlaceholder.PassingScoreSnapshot ?? assessment.PassingScore;
                    var weight = pendingPlaceholder.WeightSnapshot ?? assessment.Weight;

                    // Đây là lần nhập điểm ĐẦU TIÊN của học viên (chưa có session nào).
                    // Ta lấy luôn dòng nháp để cập nhật, ĐẢM BẢO nó không bị sinh rác.
                    pendingPlaceholder.SessionId = request.SessionId;
                    pendingPlaceholder.Score = request.Score;
                    pendingPlaceholder.ResultStatus = request.Score >= passingScore ? "Passed" : "Failed";
                    pendingPlaceholder.PassingScoreSnapshot ??= passingScore;
                    pendingPlaceholder.WeightSnapshot ??= weight;
                    pendingPlaceholder.Remark = request.Remark;
                    pendingPlaceholder.GradedByAccountId = recordedByAccountId;
                    pendingPlaceholder.RecordedAt = DateTime.UtcNow;
                    pendingPlaceholder.UpdatedAt = DateTime.UtcNow;
                    pendingPlaceholder.UpdatedByAccountId = recordedByAccountId;

                    _unitOfWork.AssessmentResultRepository.Update(pendingPlaceholder);
                    result = pendingPlaceholder;
                }
                else
                {
                    // Đây là lần thi lại HOẶC là nhập điểm ở một buổi mới.
                    // Khớp nghiêm ngặt theo session: nhập điểm ở một buổi mới (dù cùng assessment)
                    // là một lần thi mới (AttemptNo = 1), KHÔNG bị coi là thi lại của điểm buổi khác.
                    var latestResult = allResults
                        .Where(r => r.AssessmentId == request.AssessmentId && r.AccountId == request.AccountId
                            && r.SessionId == request.SessionId)
                        .OrderByDescending(r => r.AttemptNo)
                        .FirstOrDefault();

                    int attemptNo = 1;

                    // Every attempt in this chain (retakes, edits) grades against the SAME snapshot
                    // the earliest recorded attempt used; only a genuinely first-ever record for this
                    // assessment+account+session (no prior row at all) falls back to the live value.
                    var passingScore = latestResult?.PassingScoreSnapshot ?? assessment.PassingScore;
                    var weight = latestResult?.WeightSnapshot ?? assessment.Weight;

                    if (latestResult != null)
                    {
                        // [FIX] Nếu điểm của session này CHƯA được publish, giảng viên có quyền sửa đi sửa lại
                        // (VD: Frontend gọi Save nhiều lần, hoặc sửa lỗi gõ nhầm). Ta sẽ UPSERT dòng hiện tại
                        // thay vì ném lỗi bắt buộc phải có giấy phép thi lại (Retake Authorization).
                        if (!latestResult.IsPublished)
                        {
                            latestResult.Score = request.Score;
                            latestResult.ResultStatus = request.Score >= passingScore ? "Passed" : "Failed";
                            latestResult.PassingScoreSnapshot ??= passingScore;
                            latestResult.WeightSnapshot ??= weight;
                            latestResult.Remark = request.Remark;
                            latestResult.UpdatedAt = DateTime.UtcNow;
                            latestResult.UpdatedByAccountId = recordedByAccountId;

                            _unitOfWork.AssessmentResultRepository.Update(latestResult);
                            await _unitOfWork.SaveAsync(ct);

                            await CalculateSubjectResultScoreAsync(request.SubjectResultId, ct);
                            await _unitOfWork.SaveAsync(ct);
                            await _unitOfWork.CommitTransactionAsync(ct);

                            return new AssessmentResultResponse(latestResult.AssessmentResultId, latestResult.AssessmentId, latestResult.AccountId, latestResult.SubjectResultId, latestResult.SessionId, latestResult.Score, latestResult.ResultStatus, latestResult.GradedByAccountId, latestResult.RecordedAt, latestResult.PublishedAt, latestResult.IsPublished, latestResult.TakenAt, latestResult.Remark);
                        }

                        // Nếu đã Publish, đây chính thức là một lần THI LẠI (Retake)
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

                    result = new AssessmentResult
                    {
                        AssessmentId = request.AssessmentId,
                        AccountId = request.AccountId,
                        SubjectResultId = request.SubjectResultId,
                        SessionId = request.SessionId,
                        Score = request.Score,
                        ResultStatus = request.Score >= passingScore ? "Passed" : "Failed",
                        PassingScoreSnapshot = passingScore,
                        WeightSnapshot = weight,
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
                }

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

        // Nhan's fix creates multiple rows for new sessions, and Retakes create multiple rows.
        // We MUST group by AssessmentId and take only the latest recorded score for each assessment
        // to prevent artificially summing a test multiple times and skewing the average.
        var latestResults = allAssessmentResults
            .GroupBy(r => r.AssessmentId)
            .Select(g => g.OrderByDescending(r => r.RecordedAt).ThenByDescending(r => r.AttemptNo).First())
            .ToList();

        decimal totalWeightedScore = 0;
        decimal totalWeight = 0;

        foreach (var result in latestResults)
        {
            // Same rationale as PassingScoreSnapshot: use the Weight that was in force when this
            // attempt was recorded, not whatever Assessment.Weight currently is — otherwise editing
            // a Weight later would silently recompute every previously-graded learner's average the
            // next time any one of their assessments is touched.
            var assessment = allAssessments.FirstOrDefault(a => a.AssessmentId == result.AssessmentId);
            var weight = result.WeightSnapshot ?? assessment?.Weight;
            if (weight.HasValue)
            {
                totalWeightedScore += result.Score * weight.Value;
                totalWeight += weight.Value;
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

        var oldScore = result.Score;
        
        result.Score = request.Score;
        result.Remark = request.Remark;
        
        // Only set GradedByAccountId if it's currently 0 or the score actually changed,
        // or just don't overwrite if someone else imported it, wait, instruction says:
        // "không ghi đè GradedByAccountId khi bản ghi đã có người nhập (giữ dấu vết ai import ban đầu)"
        if (result.GradedByAccountId == 0)
        {
            result.GradedByAccountId = updatedByAccountId;
        }

        if (oldScore != request.Score)
        {
            await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
            {
                AccountId = updatedByAccountId,
                ActionType = AuditActionType.UPDATE.ToString(),
                EntityName = "AssessmentResult",
                RecordId = id,
                OldValue = oldScore.ToString(),
                NewValue = request.Score.ToString(),
                Description = $"Updated AssessmentResult score from {oldScore} to {request.Score}",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        var assessment = await _unitOfWork.AssessmentRepository.GetByIdAsync(result.AssessmentId, cancellationToken);
        var passingScore = result.PassingScoreSnapshot ?? assessment?.PassingScore;
        if (passingScore.HasValue)
        {
            result.ResultStatus = request.Score >= passingScore.Value ? "Passed" : "Failed";
            result.PassingScoreSnapshot ??= passingScore;
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

                // "Sân nhà ai nấy đá" — Instructor can only sign off a Subject belonging to a class
                // they are actually assigned to (see ClassOwnershipValidator).
                var etrForSignoff = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(subjectResult.EtrId, ct);
                var enrollmentForSignoff = etrForSignoff != null
                    ? await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etrForSignoff.EnrollmentId, ct)
                    : null;
                var classForSignoff = enrollmentForSignoff != null
                    ? await _unitOfWork.ClassRepository.GetByIdAsync(enrollmentForSignoff.ClassId, ct)
                    : null;
                
                var isAssignedSignoff = classForSignoff != null && _unitOfWork.ClassSubjectRepository.GetQueryable()
                    .Any(cs => cs.ClassId == classForSignoff.ClassId && cs.SubjectId == subjectResult.SubjectId && cs.InstructorAccountId == signoffByAccountId);
                ClassOwnershipValidator.EnsureInstructorOwnsSubject(signoffByRoleName, isAssignedSignoff);

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

        // Grade against the snapshot taken at Enroll time — NOT whatever CourseSubject.PassingScore
        // currently is. This matters most on RE-evaluation (e.g. Instructor re-signs off after an
        // Amendment reopened this Subject): without the snapshot, a threshold change made in between
        // would silently flip the verdict for data the learner submitted under the old rule.
        var courseSubject = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
            .FirstOrDefault(cs => cs.CourseId == subjectResult.CourseId && cs.SubjectId == subjectResult.SubjectId);
        var passingScore = subjectResult.PassingScoreSnapshot ?? courseSubject?.PassingScore ?? 50m;
        subjectResult.PassingScoreSnapshot ??= passingScore;

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

        subjectResult.Status = isPassable ? SubjectResultStatus.Passed : SubjectResultStatus.Failed;
        subjectResult.EvaluatedAt = DateTime.UtcNow;
        _unitOfWork.SubjectResultRepository.Update(subjectResult);
    }
}
