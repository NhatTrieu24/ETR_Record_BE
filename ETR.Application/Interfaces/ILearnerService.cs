using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface ILearnerService
{
    Task<LearnerResponse> CreateLearnerAsync(
        string learnerCode,
        string fullName,
        string idNumber,
        int learnerTypeId,
        CancellationToken cancellationToken = default);
}
