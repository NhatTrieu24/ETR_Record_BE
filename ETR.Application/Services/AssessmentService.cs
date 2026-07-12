using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class AssessmentService : IAssessmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public AssessmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AssessmentResultResponse> RecordAssessmentScoreAsync(CreateAssessmentResultRequest request, CancellationToken cancellationToken = default)
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

                // Retake Policy: Check if there's an existing result
                var allResults = await _unitOfWork.AssessmentResultRepository.GetAllAsync(ct);
                var existingResult = allResults.FirstOrDefault(r => 
                    r.AssessmentId == request.AssessmentId && 
                    r.LearnerId == request.LearnerId);

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
                        AuthorizedBy = request.RecordedBy,
                        AttemptNo = attemptNo,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = request.RecordedBy
                    };
                    await _unitOfWork.RetakeHistoryRepository.AddAsync(retakeHistory, ct);

                    // We soft-delete the old result or simply mark it as retaken. Since the index is unique on (AssessmentId, LearnerId),
                    // we must update the existing record rather than insert a new one if it has a unique index, 
                    // OR we must soft delete the old one. Wait, let's update the existing one.
                    existingResult.Score = request.Score;
                    existingResult.ResultStatus = request.Score >= assessment.PassingScore ? "Passed" : "Failed";
                    existingResult.Remark = request.Remark;
                    existingResult.RecordedBy = request.RecordedBy;
                    existingResult.RecordedAt = DateTime.UtcNow;
                    existingResult.AttemptNo = attemptNo;
                    existingResult.UpdatedAt = DateTime.UtcNow;
                    existingResult.UpdatedBy = request.RecordedBy;
                    
                    _unitOfWork.AssessmentResultRepository.Update(existingResult);
                }
                else
                {
                    var result = new AssessmentResult
                    {
                        AssessmentId = request.AssessmentId,
                        LearnerId = request.LearnerId,
                        SubjectResultId = request.SubjectResultId,
                        Score = request.Score,
                        ResultStatus = request.Score >= assessment.PassingScore ? "Passed" : "Failed",
                        Remark = request.Remark,
                        RecordedBy = request.RecordedBy,
                        RecordedAt = DateTime.UtcNow,
                        AttemptNo = attemptNo,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = request.RecordedBy
                    };
                    await _unitOfWork.AssessmentResultRepository.AddAsync(result, ct);
                    existingResult = result;
                }
                
                await _unitOfWork.SaveAsync(ct);

                // Auto-Calculate Total Score
                await CalculateSubjectResultScoreAsync(request.SubjectResultId, ct);
                await _unitOfWork.SaveAsync(ct);

                await _unitOfWork.CommitTransactionAsync(ct);

                return new AssessmentResultResponse(existingResult.AssessmentResultId, existingResult.AssessmentId, existingResult.LearnerId, existingResult.SubjectResultId, existingResult.Score, existingResult.ResultStatus, existingResult.RecordedBy, existingResult.RecordedAt, existingResult.PublishedAt, existingResult.IsPublished, existingResult.TakenAt, existingResult.Remark);
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

    public async Task<SubjectSignoffResponse> SignoffSubjectResultAsync(CreateSubjectSignoffRequest request, CancellationToken cancellationToken = default)
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
                    SignoffBy = request.SignoffBy,
                    Role = request.Role,
                    Comment = request.Comment,
                    SignoffAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.SignoffBy
                };

                await _unitOfWork.SubjectSignoffRepository.AddAsync(signoff, ct);
                await _unitOfWork.SaveAsync(ct);

                // Evaluate Passing Conditions (Strict Gateway)
                await EvaluateSubjectPassabilityAsync(subjectResult.SubjectResultId, ct);
                
                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new SubjectSignoffResponse(signoff.SubjectSignoffId, signoff.SubjectResultId, signoff.SignoffBy, signoff.Role, signoff.SignoffAt, signoff.Comment);
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
