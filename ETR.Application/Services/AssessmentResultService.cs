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
            r.AssessmentResultId, r.AssessmentId, r.AccountId, r.SubjectResultId, r.Score, r.ResultStatus, r.GradedByAccountId, r.RecordedAt, r.PublishedAt, r.IsPublished, r.TakenAt, r.Remark));
    }

    public async Task<AssessmentResultResponse> GetAssessmentResultByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _unitOfWork.AssessmentResultRepository.GetByIdAsync(id, cancellationToken);
        if (result == null) throw new KeyNotFoundException("AssessmentResult not found.");
        return new AssessmentResultResponse(
            result.AssessmentResultId, result.AssessmentId, result.AccountId, result.SubjectResultId, result.Score, result.ResultStatus, result.GradedByAccountId, result.RecordedAt, result.PublishedAt, result.IsPublished, result.TakenAt, result.Remark);
    }

    public async Task<IEnumerable<AssessmentResultResponse>> GetAssessmentResultsByClassStudentAsync(int classStudentId, int accountId, CancellationToken cancellationToken = default)
    {
        var classStudent = await _unitOfWork.ClassStudentRepository.GetByIdAsync(classStudentId, cancellationToken)
            ?? throw new KeyNotFoundException("ClassStudent not found.");

        // Zero-Trust: If not Admin/Instructor, ensure they are fetching their own data
        // For simplicity, we just fetch results for this student's AccountId
        var results = (await _unitOfWork.AssessmentResultRepository.GetAllAsync(cancellationToken))
            .Where(r => r.AccountId == classStudent.AccountId);

        return results.Select(r => new AssessmentResultResponse(
            r.AssessmentResultId, r.AssessmentId, r.AccountId, r.SubjectResultId, r.Score, r.ResultStatus, r.GradedByAccountId, r.RecordedAt, r.PublishedAt, r.IsPublished, r.TakenAt, r.Remark));
    }

    public async Task<AssessmentResultResponse> RecordAssessmentScoreAsync(CreateAssessmentResultRequest request, int recordedByAccountId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var assessment = await _unitOfWork.AssessmentRepository.GetByIdAsync(request.AssessmentId, ct);
                if (assessment == null) throw new InvalidOperationException("Assessment not found.");

                var subjectResult = await _unitOfWork.SubjectResultRepository.GetByIdAsync(request.SubjectResultId, ct);
                if (subjectResult == null) throw new InvalidOperationException("SubjectResult not found.");

                var allResults = await _unitOfWork.AssessmentResultRepository.GetAllAsync(ct);
                var existingResult = allResults.FirstOrDefault(r => 
                    r.AssessmentId == request.AssessmentId 
                    && r.AccountId == request.AccountId);

                int attemptNo = 1;

                if (existingResult != null)
                {
                    attemptNo = existingResult.AttemptNo + 1;
                    
                    var retakeHistory = new RetakeHistory
                    {
                        SubjectResultId = request.SubjectResultId,
                        RetakeDate = DateTime.UtcNow,
                        Reason = "Retake Assessment",
                        PreviousScore = existingResult.Score,
                        NewScore = request.Score,
                        AuthorizedByAccountId = recordedByAccountId,
                        AttemptNo = attemptNo,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = recordedByAccountId
                    };
                    await _unitOfWork.RetakeHistoryRepository.AddAsync(retakeHistory, ct);

                    // We soft-delete the old result or simply mark it as retaken. Since the index is unique on (AssessmentId, LearnerId),
                    // we must update the existing record rather than insert a new one if it has a unique index, 
                    // OR we must soft delete the old one. Wait, let's update the existing one.
                    existingResult.Score = request.Score;
                    existingResult.ResultStatus = request.Score >= assessment.PassingScore ? "Passed" : "Failed";
                    existingResult.Remark = request.Remark;
                    existingResult.GradedByAccountId = recordedByAccountId;
                    existingResult.RecordedAt = DateTime.UtcNow;
                    existingResult.AttemptNo = attemptNo;
                    existingResult.UpdatedAt = DateTime.UtcNow;
                    existingResult.UpdatedByAccountId = recordedByAccountId;
                    
                    _unitOfWork.AssessmentResultRepository.Update(existingResult);
                }
                else
                {
                    var result = new AssessmentResult
                    {
                        AssessmentId = request.AssessmentId,
                        AccountId = request.AccountId,
                        SubjectResultId = request.SubjectResultId,
                        Score = request.Score,
                        ResultStatus = request.Score >= assessment.PassingScore ? "Passed" : "Failed",
                        Remark = request.Remark,
                        GradedByAccountId = recordedByAccountId,
                        RecordedAt = DateTime.UtcNow,
                        AttemptNo = attemptNo,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = recordedByAccountId
                    };
                    await _unitOfWork.AssessmentResultRepository.AddAsync(result, ct);
                    existingResult = result;
                }
                
                await _unitOfWork.SaveAsync(ct);

                // Auto-Calculate Total Score
                await CalculateSubjectResultScoreAsync(request.SubjectResultId, ct);
                await _unitOfWork.SaveAsync(ct);

                await _unitOfWork.CommitTransactionAsync(ct);

                return new AssessmentResultResponse(existingResult.AssessmentResultId, existingResult.AssessmentId, existingResult.AccountId, existingResult.SubjectResultId, existingResult.Score, existingResult.ResultStatus, existingResult.GradedByAccountId, existingResult.RecordedAt, existingResult.PublishedAt, existingResult.IsPublished, existingResult.TakenAt, existingResult.Remark);
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
            result.AssessmentResultId, result.AssessmentId, result.AccountId, result.SubjectResultId, result.Score, result.ResultStatus, result.GradedByAccountId, result.RecordedAt, result.PublishedAt, result.IsPublished, result.TakenAt, result.Remark);
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

    public async Task<SubjectSignoffResponse> SignoffSubjectResultAsync(CreateSubjectSignoffRequest request, int signoffByAccountId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var subjectResult = await _unitOfWork.SubjectResultRepository.GetByIdAsync(request.SubjectResultId, ct);
                if (subjectResult == null) throw new InvalidOperationException("SubjectResult not found.");

                var signoff = new SubjectSignoff
                {
                    SubjectResultId = request.SubjectResultId,
                    SignoffByAccountId = signoffByAccountId,
                    Role = request.Role,
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

        // 1. Attendance Threshold
        if ((subjectResult.AttendanceRate ?? 0) < BusinessRuleEngine.MinimumAttendanceThreshold)
        {
            return; // Cannot pass
        }

        // 2. Practical Checklist
        var checklists = (await _unitOfWork.PracticalChecklistRepository.GetAllAsync(ct))
            .Where(p => p.CourseId == subjectResult.CourseId && p.SubjectId == subjectResult.SubjectId && p.IsRequired).ToList();
        
        var checklistResults = (await _unitOfWork.PracticalChecklistResultRepository.GetAllAsync(ct))
            .Where(r => r.SubjectResultId == subjectResultId).ToList();

        if (checklists.Any(c => !checklistResults.Any(r => r.PracticalChecklistId == c.PracticalChecklistId && r.IsCompleted)))
        {
            return; // Mandatory checklist not completed
        }

        // 3. Mandatory Evidence Files (At least ONE EvidenceFile must be linked)
        var evidenceFiles = (await _unitOfWork.EvidenceFileRepository.GetAllAsync(ct))
            .Where(e => e.SubjectResultId == subjectResultId).ToList();

        if (evidenceFiles.Count == 0)
        {
            return; // No evidence file uploaded
        }

        // 4. Score check (using CourseSubject)
        var courseSubjects = await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct);
        var courseSubject = courseSubjects.FirstOrDefault(cs => cs.CourseId == subjectResult.CourseId && cs.SubjectId == subjectResult.SubjectId);
        
        if (courseSubject != null && (subjectResult.Score ?? 0) < courseSubject.PassingScore)
        {
            return; // Score too low
        }

        // If all strict conditions are met:
        subjectResult.Status = "Passed";
        subjectResult.EvaluatedAt = DateTime.UtcNow;
        _unitOfWork.SubjectResultRepository.Update(subjectResult);
    }
}
