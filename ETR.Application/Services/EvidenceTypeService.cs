using ETR.Application.DTOs.EvidenceType;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class EvidenceTypeService : IEvidenceTypeService
{
    private readonly IUnitOfWork _unitOfWork;

    public EvidenceTypeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<EvidenceTypeResponse>> GetAllEvidenceTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await _unitOfWork.EvidenceTypeRepository.GetAllAsync(cancellationToken);
        return types.Select(d => new EvidenceTypeResponse
        {
            EvidenceTypeId = d.EvidenceTypeId,
            TypeName = d.TypeName,
            Description = d.Description
        });
    }

    public async Task<EvidenceTypeResponse> GetEvidenceTypeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var d = await _unitOfWork.EvidenceTypeRepository.GetByIdAsync(id, cancellationToken);
        if (d == null) throw new KeyNotFoundException("EvidenceType not found.");

        return new EvidenceTypeResponse
        {
            EvidenceTypeId = d.EvidenceTypeId,
            TypeName = d.TypeName,
            Description = d.Description
        };
    }

    public async Task<EvidenceTypeResponse> CreateEvidenceTypeAsync(CreateEvidenceTypeRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        var evidenceType = new EvidenceType
        {
            TypeName = request.TypeName,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.EvidenceTypeRepository.AddAsync(evidenceType, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EvidenceTypeResponse
        {
            EvidenceTypeId = evidenceType.EvidenceTypeId,
            TypeName = evidenceType.TypeName,
            Description = evidenceType.Description
        };
    }

    public async Task<EvidenceTypeResponse> UpdateEvidenceTypeAsync(int id, UpdateEvidenceTypeRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var evidenceType = await _unitOfWork.EvidenceTypeRepository.GetByIdAsync(id, cancellationToken);
        if (evidenceType == null) throw new KeyNotFoundException("EvidenceType not found.");

        evidenceType.TypeName = request.TypeName;
        evidenceType.Description = request.Description;
        evidenceType.UpdatedAt = DateTime.UtcNow;
        evidenceType.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.EvidenceTypeRepository.Update(evidenceType);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EvidenceTypeResponse
        {
            EvidenceTypeId = evidenceType.EvidenceTypeId,
            TypeName = evidenceType.TypeName,
            Description = evidenceType.Description
        };
    }

    public async Task DeleteEvidenceTypeAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var evidenceType = await _unitOfWork.EvidenceTypeRepository.GetByIdAsync(id, cancellationToken);
        if (evidenceType == null) throw new KeyNotFoundException("EvidenceType not found.");

        evidenceType.IsDeleted = true;
        evidenceType.DeletedAt = DateTime.UtcNow;
        evidenceType.UpdatedAt = DateTime.UtcNow;
        evidenceType.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.EvidenceTypeRepository.Update(evidenceType);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
