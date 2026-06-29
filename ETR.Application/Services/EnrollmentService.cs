using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ChecklistProgressInitializer _checklistProgressInitializer;

    public EnrollmentService(
        IUnitOfWork unitOfWork,
        ChecklistProgressInitializer checklistProgressInitializer)
    {
        _unitOfWork = unitOfWork;
        _checklistProgressInitializer = checklistProgressInitializer;
    }

    public async Task<CreateEnrollmentResult> CreateEnrollmentAsync(
        int learnerId,
        int classId,
        int createdByUserId,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var enrollment = new Enrollment
                {
                    LearnerId = learnerId,
                    ClassId = classId,
                    Status = "Active",
                    EnrolledAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdByUserId
                };

                await _unitOfWork.EnrollmentRepository.AddAsync(enrollment, ct);
                await _unitOfWork.SaveAsync(ct);

                var etrRecord = new ETRRecord
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    Status = "InProgress",
                    IsLocked = false,
                    CreatedBySystem = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdByUserId
                };

                await _unitOfWork.ETRRecordRepository.AddAsync(etrRecord, ct);
                await _unitOfWork.SaveAsync(ct);

                await _checklistProgressInitializer.InitializeForEtrRecordAsync(
                    etrRecord.ETRRecordId,
                    classId,
                    createdByUserId,
                    ct);

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new CreateEnrollmentResult(enrollment, etrRecord);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }
}
