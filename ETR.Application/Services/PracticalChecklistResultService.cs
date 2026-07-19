using ETR.Application.DTOs.PracticalChecklistResult;
using ETR.Application.Interfaces;
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

    public async Task<IEnumerable<PracticalChecklistResultResponse>> GetAllPracticalChecklistResultsAsync(CancellationToken cancellationToken = default)
    {
        var results = await _unitOfWork.PracticalChecklistResultRepository.GetAllAsync(cancellationToken);
        return results.Select(r => new PracticalChecklistResultResponse(
            r.PracticalChecklistResultId,
            r.SessionId,
            r.SubjectResultId,
            r.PracticalChecklistId,
            r.IsCompleted,
            r.VerifiedByAccountId,
            r.CompletedAt,
            r.VerificationComment
        ));
    }

    public async Task<PracticalChecklistResultResponse> GetPracticalChecklistResultByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _unitOfWork.PracticalChecklistResultRepository.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            throw new KeyNotFoundException($"PracticalChecklistResult with ID {id} not found.");
        }

        return new PracticalChecklistResultResponse(
            result.PracticalChecklistResultId,
            result.SessionId,
            result.SubjectResultId,
            result.PracticalChecklistId,
            result.IsCompleted,
            result.VerifiedByAccountId,
            result.CompletedAt,
            result.VerificationComment
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

        result.IsCompleted = request.IsCompleted;
        result.VerificationComment = request.VerificationComment;
        result.VerifiedByAccountId = verifiedByAccountId;
        result.CompletedAt = request.IsCompleted ? DateTime.UtcNow : null;

        _unitOfWork.PracticalChecklistResultRepository.Update(result);
        await _unitOfWork.SaveAsync(cancellationToken);

        _logger.LogInformation("PracticalChecklistResult {Id} updated by Account {AccountId}", id, verifiedByAccountId);

        return new PracticalChecklistResultResponse(
            result.PracticalChecklistResultId,
            result.SessionId,
            result.SubjectResultId,
            result.PracticalChecklistId,
            result.IsCompleted,
            result.VerifiedByAccountId,
            result.CompletedAt,
            result.VerificationComment
        );
    }
}
