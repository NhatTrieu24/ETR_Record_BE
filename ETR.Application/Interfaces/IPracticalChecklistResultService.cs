using ETR.Application.DTOs.PracticalChecklistResult;

namespace ETR.Application.Interfaces;

public interface IPracticalChecklistResultService
{
    Task<IEnumerable<PracticalChecklistResultResponse>> GetAllPracticalChecklistResultsAsync(CancellationToken cancellationToken = default);
    Task<PracticalChecklistResultResponse> GetPracticalChecklistResultByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PracticalChecklistResultResponse> UpdatePracticalChecklistResultAsync(int id, UpdatePracticalChecklistResultRequest request, int verifiedByAccountId, CancellationToken cancellationToken = default);
}
