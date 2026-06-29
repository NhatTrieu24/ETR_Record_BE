using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class EtrService : IEtrService
{
    private readonly IUnitOfWork _unitOfWork;

    public EtrService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // 1. Cập nhật tiến độ Checklist
    public async Task<ETRChecklistProgress> UpdateChecklistProgressAsync(
        int progressId,
        bool isCompleted,
        int? verifiedByUserId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var progress = await _unitOfWork.ETRChecklistProgressRepository.GetByIdAsync(progressId, cancellationToken)
            ?? throw new KeyNotFoundException($"Checklist progress {progressId} was not found.");

        progress.IsCompleted = isCompleted;
        progress.CompletedAt = isCompleted ? DateTime.UtcNow : null;
        progress.VerifiedBy = verifiedByUserId;
        progress.VerificationComment = comment;
        progress.UpdatedAt = DateTime.UtcNow;
        progress.UpdatedBy = verifiedByUserId;

        _unitOfWork.ETRChecklistProgressRepository.Update(progress);
        await _unitOfWork.SaveAsync(cancellationToken);

        return progress;
    }

    // 2. Nộp hồ sơ ETR
    public async Task<ETRRecord> SubmitEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRRecordRepository.GetByIdAsync(etrRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ ETR với ID {etrRecordId}.");

        if (etr.Status != "InProgress" && etr.Status != "ReturnedForCorrection")
            throw new InvalidOperationException("Hồ sơ chỉ được nộp khi đang ở trạng thái InProgress hoặc ReturnedForCorrection.");

        etr.Status = "Submitted";
        etr.SubmittedAt = DateTime.UtcNow;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedBy = userId;

        _unitOfWork.ETRRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return etr;
    }

    // 3. Xác minh hồ sơ ETR (QA)
    public async Task<ETRRecord> VerifyEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRRecordRepository.GetByIdAsync(etrRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ ETR với ID {etrRecordId}.");

        if (etr.Status != "Submitted")
            throw new InvalidOperationException("Hồ sơ chỉ được xác minh khi đang ở trạng thái Submitted.");

        etr.Status = "Verified";
        etr.VerifiedAt = DateTime.UtcNow;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedBy = userId;

        _unitOfWork.ETRRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return etr;
    }

    // 4. Hoàn thành & Khóa cứng hồ sơ ETR
    public async Task<ETRRecord> CompleteEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRRecordRepository.GetByIdAsync(etrRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ ETR với ID {etrRecordId}.");

        if (etr.Status != "Verified")
            throw new InvalidOperationException("Hồ sơ chỉ được hoàn thành khi đã được QA Verified.");

        etr.Status = "Completed";
        etr.CompletedAt = DateTime.UtcNow;
        etr.IsLocked = true; // Kích hoạt bộ lọc Immutability Guard bảo vệ hồ sơ

        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedBy = userId;

        _unitOfWork.ETRRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return etr;
    }
}