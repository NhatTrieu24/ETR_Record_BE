using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class LearnerService : ILearnerService
{
    private readonly IUnitOfWork _unitOfWork;

    public LearnerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Learner> CreateLearnerAsync(
        string learnerCode,
        string fullName,
        string idNumber,
        int learnerTypeId,
        CancellationToken cancellationToken = default)
    {
        var learner = new Learner
        {
            LearnerCode = learnerCode,
            FullName = fullName,
            IdentificationNumber = idNumber,
            LearnerTypeId = learnerTypeId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.LearnerRepository.AddAsync(learner, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return learner;
    }
}
