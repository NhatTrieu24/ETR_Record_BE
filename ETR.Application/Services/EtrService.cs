using ETR.Application.DTOs;
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
    public async Task<ChecklistProgressResponse> UpdateChecklistProgressAsync(
        int progressId,
        bool isCompleted,
        int? verifiedByUserId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var progress = await _unitOfWork.ETRChecklistProgressRepository.GetByIdAsync(progressId, cancellationToken)
            ?? throw new KeyNotFoundException($"Checklist progress {progressId} was not found.");

        var etrRecord = await _unitOfWork.ETRRecordRepository.GetByIdAsync(progress.ETRRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETR Record {progress.ETRRecordId} was not found.");

        if (etrRecord.IsLocked)
        {
            throw new InvalidOperationException("Không thể cập nhật tiến độ checklist vì hồ sơ ETR đã bị khóa.");
        }

        progress.IsCompleted = isCompleted;
        progress.CompletedAt = isCompleted ? DateTime.UtcNow : null;
        progress.VerifiedBy = verifiedByUserId;
        progress.VerificationComment = comment;
        progress.UpdatedAt = DateTime.UtcNow;
        progress.UpdatedBy = verifiedByUserId;

        _unitOfWork.ETRChecklistProgressRepository.Update(progress);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new ChecklistProgressResponse(
            progress.ProgressId,
            progress.ETRRecordId,
            progress.ChecklistItemId,
            progress.IsCompleted,
            progress.VerifiedBy,
            progress.CompletedAt,
            progress.VerificationComment);
    }

    // 2. Nộp hồ sơ ETR
    public async Task<EtrRecordResponse> SubmitEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRRecordRepository.GetByIdAsync(etrRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ ETR với ID {etrRecordId}.");

        if (etr.IsLocked)
            throw new InvalidOperationException("Không thể nộp hồ sơ ETR vì hồ sơ đã bị khóa.");

        if (etr.Status != "InProgress" && etr.Status != "ReturnedForCorrection")
            throw new InvalidOperationException("Hồ sơ chỉ được nộp khi đang ở trạng thái InProgress hoặc ReturnedForCorrection.");

        etr.Status = "Submitted";
        etr.SubmittedAt = DateTime.UtcNow;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedBy = userId;

        _unitOfWork.ETRRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapEtrToResponse(etr);
    }

    // 3. Xác minh hồ sơ ETR (QA)
    public async Task<EtrRecordResponse> VerifyEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRRecordRepository.GetByIdAsync(etrRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ ETR với ID {etrRecordId}.");

        if (etr.IsLocked)
            throw new InvalidOperationException("Không thể xác minh hồ sơ ETR vì hồ sơ đã bị khóa.");

        if (etr.Status != "Submitted")
            throw new InvalidOperationException("Hồ sơ chỉ được xác minh khi đang ở trạng thái Submitted.");

        etr.Status = "Verified";
        etr.VerifiedAt = DateTime.UtcNow;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedBy = userId;

        _unitOfWork.ETRRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapEtrToResponse(etr);
    }

    // 4. Hoàn thành & Khóa cứng hồ sơ ETR
    public async Task<EtrRecordResponse> CompleteEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRRecordRepository.GetByIdAsync(etrRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ ETR với ID {etrRecordId}.");

        if (etr.IsLocked)
            throw new InvalidOperationException("Không thể hoàn thành hồ sơ ETR vì hồ sơ đã bị khóa.");

        if (etr.Status != "Verified")
            throw new InvalidOperationException("Hồ sơ chỉ được hoàn thành khi đã được QA Verified.");

        etr.Status = "Completed";
        etr.CompletedAt = DateTime.UtcNow;
        etr.IsLocked = true; // Kích hoạt bộ lọc Immutability Guard bảo vệ hồ sơ

        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedBy = userId;

        _unitOfWork.ETRRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapEtrToResponse(etr);
    }

    // 5. Khóa hồ sơ ETR
    public async Task<EtrRecordResponse> LockEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRRecordRepository.GetByIdAsync(etrRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ ETR với ID {etrRecordId}.");

        etr.IsLocked = true;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedBy = userId;

        _unitOfWork.ETRRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapEtrToResponse(etr);
    }

    // 6. Mở khóa hồ sơ ETR
    public async Task<EtrRecordResponse> UnlockEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRRecordRepository.GetByIdAsync(etrRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ ETR với ID {etrRecordId}.");

        etr.IsLocked = false;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedBy = userId;

        _unitOfWork.ETRRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapEtrToResponse(etr);
    }

    private static EtrRecordResponse MapEtrToResponse(ETRRecord e)
    {
        return new EtrRecordResponse(
            e.ETRRecordId,
            e.EnrollmentId,
            e.Status,
            e.IsLocked,
            e.SubmittedAt,
            e.VerifiedAt,
            e.CompletedAt);
    }
}