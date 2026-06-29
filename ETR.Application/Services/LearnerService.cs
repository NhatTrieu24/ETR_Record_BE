using ETR.Application.DTOs;
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

    public async Task<LearnerResponse> CreateLearnerAsync(
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

        return new LearnerResponse(
            learner.LearnerId,
            learner.LearnerCode,
            learner.FullName,
            learner.DateOfBirth,
            learner.Gender,
            learner.Phone,
            learner.Email,
            learner.IdentificationNumber ?? string.Empty,
            learner.Organization,
            learner.Status,
            learner.LearnerTypeId);
    }
}
