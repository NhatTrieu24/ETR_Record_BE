using ETR.Domain.Entities;

namespace ETR.Application.Interfaces;

public interface ILearnerService
{
    Task<Learner> CreateLearnerAsync(
        string learnerCode,
        string fullName,
        string idNumber,
        int learnerTypeId,
        CancellationToken cancellationToken = default);
}
