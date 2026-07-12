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
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.RecordedBy
                };

                await _unitOfWork.AssessmentResultRepository.AddAsync(result, ct);
                await _unitOfWork.SaveAsync(ct);

                await _unitOfWork.CommitTransactionAsync(ct);

                return new AssessmentResultResponse(result.AssessmentResultId, result.AssessmentId, result.LearnerId, result.SubjectResultId, result.Score, result.ResultStatus, result.RecordedBy, result.RecordedAt, result.PublishedAt, result.IsPublished, result.TakenAt, result.Remark);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
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
                
                subjectResult.Status = "Passed"; 
                _unitOfWork.SubjectResultRepository.Update(subjectResult);
                
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
}
