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
            throw new InvalidOperationException("SubjectResult not found.");

        // Find the first PracticalChecklist for this course/subject
        var checklist = (await _unitOfWork.PracticalChecklistRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(pc => pc.CourseId == subjectResult.CourseId && pc.SubjectId == subjectResult.SubjectId);
        
        // Auto-create a default PracticalChecklist if none exists
        if (checklist == null)
        {
            var course = await _unitOfWork.CourseRepository.GetByIdAsync(subjectResult.CourseId, cancellationToken);
            var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(subjectResult.SubjectId, cancellationToken);
            checklist = new PracticalChecklist
            {
                CourseId = subjectResult.CourseId,
                SubjectId = subjectResult.SubjectId,
                ItemName = $"Practical - {(subject?.SubjectName ?? "Subject")}",
                Description = $"Default practical checklist for {(course?.CourseName ?? "Course")}",
                IsRequired = true,
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow,
                CreatedByAccountId = verifiedByAccountId
            };
            await _unitOfWork.PracticalChecklistRepository.AddAsync(checklist, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);
            _logger.LogInformation("Auto-created default PracticalChecklist for CourseId={CourseId}, SubjectId={SubjectId}", 
                subjectResult.CourseId, subjectResult.SubjectId);
        }

        var result = new PracticalChecklistResult
        {
            SubjectResultId = request.SubjectResultId,
            PracticalChecklistId = checklist.PracticalChecklistId,
            SessionId = request.SessionId,
            Score = request.Score,
            ResultStatus = request.Score >= 50 ? "Passed" : "Failed",
            VerificationComment = request.VerificationComment,
            VerifiedByAccountId = verifiedByAccountId,
            CompletedAt = request.Score >= 50 ? DateTime.UtcNow : null,
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

        result.Score = request.Score;
        result.ResultStatus = request.Score >= 50 ? "Passed" : "Failed";
        result.VerificationComment = request.VerificationComment;
        result.VerifiedByAccountId = verifiedByAccountId;
        result.CompletedAt = request.Score >= 50 ? DateTime.UtcNow : null;

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
            throw new InvalidOperationException("PracticalChecklistResult is already published.");
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
