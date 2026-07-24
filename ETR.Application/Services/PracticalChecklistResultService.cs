using ETR.Application.Compliance;
using ETR.Application.DTOs.PracticalChecklistResult;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ETR.Application.Services;

public class PracticalChecklistResultService : IPracticalChecklistResultService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PracticalChecklistResultService> _logger;

    public PracticalChecklistResultService(IUnitOfWork unitOfWork, ILogger<PracticalChecklistResultService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private async Task<decimal> GetPassingScoreAsync(int courseId, int subjectId, CancellationToken ct)
    {
        var courseSubject = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
            .FirstOrDefault(cs => cs.CourseId == courseId && cs.SubjectId == subjectId);
        return courseSubject?.PassingScore ?? 50m;
    }

    private async Task<int?> GetAccountIdAsync(int subjectResultId, CancellationToken ct)
    {
        var subjectResult = await _unitOfWork.SubjectResultRepository.GetByIdAsync(subjectResultId, ct);
        if (subjectResult == null) return null;

        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(subjectResult.EtrId, ct);
        if (etr == null) return null;

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, ct);
        return enrollment?.AccountId;
    }

    public async Task<IEnumerable<PracticalChecklistResultResponse>> GetAllPracticalChecklistResultsAsync(CancellationToken cancellationToken = default)
    {
        var results = await _unitOfWork.PracticalChecklistResultRepository.GetAllAsync(cancellationToken);
        var responses = new List<PracticalChecklistResultResponse>();
        foreach (var r in results)
        {
            var accountId = await GetAccountIdAsync(r.SubjectResultId, cancellationToken);
            responses.Add(new PracticalChecklistResultResponse(
                r.PracticalChecklistResultId,
                r.SessionId,
                r.SubjectResultId,
                r.PracticalChecklistId,
                r.Score,
                r.ResultStatus,
                r.VerifiedByAccountId,
                r.CompletedAt,
                r.VerificationComment,
                r.IsPublished,
                r.PublishedAt,
                accountId
            ));
        }
        return responses;
    }

    public async Task<PracticalChecklistResultResponse> GetPracticalChecklistResultByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _unitOfWork.PracticalChecklistResultRepository.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            throw new KeyNotFoundException($"PracticalChecklistResult with ID {id} not found.");
        }

        var accountId = await GetAccountIdAsync(result.SubjectResultId, cancellationToken);

        return new PracticalChecklistResultResponse(
            result.PracticalChecklistResultId,
            result.SessionId,
            result.SubjectResultId,
            result.PracticalChecklistId,
            result.Score,
            result.ResultStatus,
            result.VerifiedByAccountId,
            result.CompletedAt,
            result.VerificationComment,
            result.IsPublished,
            result.PublishedAt,
            accountId
        );
    }

    public async Task<PracticalChecklistResultResponse> CreatePracticalChecklistResultAsync(CreatePracticalChecklistResultRequest request, int verifiedByAccountId, CancellationToken cancellationToken = default)
    {
        // Look up SubjectResult to find CourseId and SubjectId
        var subjectResult = await _unitOfWork.SubjectResultRepository.GetByIdAsync(request.SubjectResultId, cancellationToken);
        if (subjectResult == null)
            throw new BusinessRuleViolationException("SubjectResult not found.");

        // Find the PracticalChecklist for this course/subject — must already be configured by Admin/Instructor.
        var checklist = (await _unitOfWork.PracticalChecklistRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(pc => pc.CourseId == subjectResult.CourseId && pc.SubjectId == subjectResult.SubjectId);

        if (checklist == null)
        {
            throw new BusinessRuleViolationException(
                $"No PracticalChecklist configured for CourseId={subjectResult.CourseId}, SubjectId={subjectResult.SubjectId}. Configure one via PracticalChecklistsController before recording a result.");
        }

        var passingScore = await GetPassingScoreAsync(subjectResult.CourseId, subjectResult.SubjectId, cancellationToken);

        var result = new PracticalChecklistResult
        {
            SubjectResultId = request.SubjectResultId,
            PracticalChecklistId = checklist.PracticalChecklistId,
            SessionId = request.SessionId,
            Score = request.Score,
            ResultStatus = request.Score >= passingScore ? "Passed" : "Failed",
            VerificationComment = request.VerificationComment,
            VerifiedByAccountId = verifiedByAccountId,
            CompletedAt = request.Score >= passingScore ? DateTime.UtcNow : null,
            IsPublished = false,
            PublishedAt = null,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = verifiedByAccountId
        };

        await _unitOfWork.PracticalChecklistResultRepository.AddAsync(result, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        _logger.LogInformation("PracticalChecklistResult created by Account {AccountId}: Id={Id}", verifiedByAccountId, result.PracticalChecklistResultId);

        var accountId = await GetAccountIdAsync(result.SubjectResultId, cancellationToken);

        return new PracticalChecklistResultResponse(
            result.PracticalChecklistResultId,
            result.SessionId,
            result.SubjectResultId,
            result.PracticalChecklistId,
            result.Score,
            result.ResultStatus,
            result.VerifiedByAccountId,
            result.CompletedAt,
            result.VerificationComment,
            result.IsPublished,
            result.PublishedAt,
            accountId
        );
    }

    public async Task<PracticalChecklistResultResponse> UpdatePracticalChecklistResultAsync(int id, UpdatePracticalChecklistResultRequest request, int verifiedByAccountId, CancellationToken cancellationToken = default)
    {
        var result = await _unitOfWork.PracticalChecklistResultRepository.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            throw new KeyNotFoundException($"PracticalChecklistResult with ID {id} not found.");
        }

        if (request.SessionId.HasValue)
        {
            result.SessionId = request.SessionId;
        }

        var subjectResult = await _unitOfWork.SubjectResultRepository.GetByIdAsync(result.SubjectResultId, cancellationToken);
        var passingScore = subjectResult == null
            ? 50m
            : await GetPassingScoreAsync(subjectResult.CourseId, subjectResult.SubjectId, cancellationToken);

        result.Score = request.Score;
        result.ResultStatus = request.Score >= passingScore ? "Passed" : "Failed";
        result.VerificationComment = request.VerificationComment;
        result.VerifiedByAccountId = verifiedByAccountId;
        result.CompletedAt = request.Score >= passingScore ? DateTime.UtcNow : null;

        _unitOfWork.PracticalChecklistResultRepository.Update(result);
        await _unitOfWork.SaveAsync(cancellationToken);

        _logger.LogInformation("PracticalChecklistResult {Id} updated by Account {AccountId}", id, verifiedByAccountId);

        var accountId = await GetAccountIdAsync(result.SubjectResultId, cancellationToken);

        return new PracticalChecklistResultResponse(
            result.PracticalChecklistResultId,
            result.SessionId,
            result.SubjectResultId,
            result.PracticalChecklistId,
            result.Score,
            result.ResultStatus,
            result.VerifiedByAccountId,
            result.CompletedAt,
            result.VerificationComment,
            result.IsPublished,
            result.PublishedAt,
            accountId
        );
    }

    public async Task<PracticalChecklistResultResponse> PublishPracticalChecklistResultAsync(int id, int publishedByAccountId, CancellationToken cancellationToken = default)
    {
        var result = await _unitOfWork.PracticalChecklistResultRepository.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            throw new KeyNotFoundException($"PracticalChecklistResult with ID {id} not found.");
        }

        if (result.IsPublished)
        {
            throw new BusinessRuleViolationException("PracticalChecklistResult is already published.");
        }

        result.IsPublished = true;
        result.PublishedAt = DateTime.UtcNow;
        result.UpdatedAt = DateTime.UtcNow;
        result.UpdatedByAccountId = publishedByAccountId;

        _unitOfWork.PracticalChecklistResultRepository.Update(result);
        await _unitOfWork.SaveAsync(cancellationToken);

        _logger.LogInformation("PracticalChecklistResult {Id} published by Account {AccountId}", id, publishedByAccountId);

        var accountId = await GetAccountIdAsync(result.SubjectResultId, cancellationToken);

        return new PracticalChecklistResultResponse(
            result.PracticalChecklistResultId,
            result.SessionId,
            result.SubjectResultId,
            result.PracticalChecklistId,
            result.Score,
            result.ResultStatus,
            result.VerifiedByAccountId,
            result.CompletedAt,
            result.VerificationComment,
            result.IsPublished,
            result.PublishedAt,
            accountId
        );
    }
}
