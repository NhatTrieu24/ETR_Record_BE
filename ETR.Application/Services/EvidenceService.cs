using ETR.Application.DTOs.Evidence;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace ETR.Application.Services;

public class EvidenceService : IEvidenceService
{
    private readonly IUnitOfWork _unitOfWork;

    public EvidenceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<EvidenceResponse>> GetAllEvidencesAsync(CancellationToken cancellationToken = default)
    {
        var evidences = await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken);
        return evidences.Select(MapToResponse).ToList();
    }

    public async Task<EvidenceResponse> GetEvidenceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var evidence = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (evidence == null)
            throw new KeyNotFoundException($"Evidence with ID {id} not found.");

        return MapToResponse(evidence);
    }

    public async Task<EvidenceResponse> UploadEvidenceAsync(CreateEvidenceRequest request, int uploadedByAccountId, CancellationToken cancellationToken = default)
    {
        var evidence = new EvidenceFile
        {
            EvidenceTypeId = request.EvidenceTypeId,
            AccountId = request.AccountId,
            SubjectResultId = request.SubjectResultId,
            AttendanceRecordId = request.AttendanceRecordId,
            AssessmentResultId = request.AssessmentResultId,
            FileName = request.FileName,
            FilePath = request.FilePath,
            FileExtension = request.FileExtension,
            MimeType = request.MimeType,
            FileSize = request.FileSize,
            VerificationStatus = "Pending", // Default value
            UploadedByAccountId = uploadedByAccountId,
            UploadedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = uploadedByAccountId
        };

        await _unitOfWork.EvidenceFileRepository.AddAsync(evidence, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(evidence);
    }

    public async Task<EvidenceResponse> UpdateEvidenceAsync(int id, ETR.Application.DTOs.UpdateEvidenceRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var evidence = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (evidence == null)
            throw new KeyNotFoundException($"Evidence with ID {id} not found.");

        evidence.EvidenceTypeId = request.EvidenceTypeId;
        evidence.FileName = request.FileName;
        evidence.FilePath = request.FilePath;
        evidence.FileExtension = request.FileExtension;
        evidence.MimeType = request.MimeType;
        evidence.FileSize = request.FileSize;
        
        evidence.UpdatedAt = DateTime.UtcNow;
        evidence.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.EvidenceFileRepository.Update(evidence);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(evidence);
    }

    public async Task DeleteEvidenceAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var evidence = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (evidence == null)
            throw new KeyNotFoundException($"Evidence with ID {id} not found.");

        // Soft delete based on BaseEntity pattern
        evidence.IsDeleted = true;
        evidence.DeletedAt = DateTime.UtcNow;
        evidence.UpdatedAt = DateTime.UtcNow;
        evidence.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.EvidenceFileRepository.Update(evidence);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private EvidenceResponse MapToResponse(EvidenceFile file)
    {
        return new EvidenceResponse
        {
            EvidenceFileId = file.EvidenceFileId,
            EvidenceTypeId = file.EvidenceTypeId,
            UploadedByAccountId = file.UploadedByAccountId,
            AccountId = file.AccountId,
            SubjectResultId = file.SubjectResultId,
            AttendanceRecordId = file.AttendanceRecordId,
            AssessmentResultId = file.AssessmentResultId,
            FileName = file.FileName,
            FilePath = file.FilePath,
            FileExtension = file.FileExtension,
            MimeType = file.MimeType,
            FileSize = file.FileSize,
            VerificationStatus = file.VerificationStatus,
            VerifiedByAccountId = file.VerifiedByAccountId,
            VerifiedAt = file.VerifiedAt,
            VerificationComment = file.VerificationComment,
            UploadedAt = file.UploadedAt
        };
    }
}
