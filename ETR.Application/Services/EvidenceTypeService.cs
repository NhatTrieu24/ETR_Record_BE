using ETR.Application.Compliance;
using ETR.Application.DTOs.EvidenceType;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;

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
            TypeCode = d.TypeCode,
            TypeName = d.TypeName,
            Description = d.Description,
            DepartmentScope = d.DepartmentScope,
            SubjectTypeScope = d.SubjectTypeScope,
            IsMandatory = d.IsMandatory,
            Category = d.Category
        });
    }

    public async Task<EvidenceTypeResponse> GetEvidenceTypeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var d = await _unitOfWork.EvidenceTypeRepository.GetByIdAsync(id, cancellationToken);
        if (d == null) throw new KeyNotFoundException("EvidenceType not found.");

        return new EvidenceTypeResponse
        {
            EvidenceTypeId = d.EvidenceTypeId,
            TypeCode = d.TypeCode,
            TypeName = d.TypeName,
            Description = d.Description,
            DepartmentScope = d.DepartmentScope,
            SubjectTypeScope = d.SubjectTypeScope,
            IsMandatory = d.IsMandatory,
            Category = d.Category
        };
    }

    public async Task<EvidenceTypeResponse> CreateEvidenceTypeAsync(CreateEvidenceTypeRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        var existingTypes = await _unitOfWork.EvidenceTypeRepository.GetAllAsync(cancellationToken);
        if (existingTypes.Any(t => t.TypeName == request.TypeName))
        {
            throw new BusinessRuleViolationException($"An evidence type named '{request.TypeName}' already exists.");
        }
        if (!string.IsNullOrWhiteSpace(request.TypeCode) && existingTypes.Any(t => t.TypeCode.Equals(request.TypeCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessRuleViolationException($"An evidence type with code '{request.TypeCode}' already exists.");
        }

        var evidenceType = new EvidenceType
        {
            TypeCode = request.TypeCode?.Trim() ?? string.Empty,
            TypeName = request.TypeName,
            Description = request.Description,
            DepartmentScope = request.DepartmentScope,
            SubjectTypeScope = request.SubjectTypeScope,
            IsMandatory = request.IsMandatory,
            Category = string.IsNullOrWhiteSpace(request.Category) ? "SubjectEvidence" : request.Category,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.EvidenceTypeRepository.AddAsync(evidenceType, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EvidenceTypeResponse
        {
            EvidenceTypeId = evidenceType.EvidenceTypeId,
            TypeCode = evidenceType.TypeCode,
            TypeName = evidenceType.TypeName,
            Description = evidenceType.Description,
            DepartmentScope = evidenceType.DepartmentScope,
            SubjectTypeScope = evidenceType.SubjectTypeScope,
            IsMandatory = evidenceType.IsMandatory,
            Category = evidenceType.Category
        };
    }

    public async Task<EvidenceTypeResponse> UpdateEvidenceTypeAsync(int id, UpdateEvidenceTypeRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var evidenceType = await _unitOfWork.EvidenceTypeRepository.GetByIdAsync(id, cancellationToken);
        if (evidenceType == null) throw new KeyNotFoundException("EvidenceType not found.");

        var existingTypes = await _unitOfWork.EvidenceTypeRepository.GetAllAsync(cancellationToken);
        if (existingTypes.Any(t => t.EvidenceTypeId != id && t.TypeName == request.TypeName))
        {
            throw new BusinessRuleViolationException($"An evidence type named '{request.TypeName}' already exists.");
        }
        if (!string.IsNullOrWhiteSpace(request.TypeCode) && existingTypes.Any(t => t.EvidenceTypeId != id && t.TypeCode.Equals(request.TypeCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessRuleViolationException($"An evidence type with code '{request.TypeCode}' already exists.");
        }

        evidenceType.TypeCode = request.TypeCode?.Trim() ?? evidenceType.TypeCode;
        evidenceType.TypeName = request.TypeName;
        evidenceType.Description = request.Description;
        evidenceType.DepartmentScope = request.DepartmentScope;
        evidenceType.SubjectTypeScope = request.SubjectTypeScope;
        evidenceType.IsMandatory = request.IsMandatory;
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            evidenceType.Category = request.Category;
        }
        evidenceType.UpdatedAt = DateTime.UtcNow;
        evidenceType.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.EvidenceTypeRepository.Update(evidenceType);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EvidenceTypeResponse
        {
            EvidenceTypeId = evidenceType.EvidenceTypeId,
            TypeCode = evidenceType.TypeCode,
            TypeName = evidenceType.TypeName,
            Description = evidenceType.Description,
            DepartmentScope = evidenceType.DepartmentScope,
            SubjectTypeScope = evidenceType.SubjectTypeScope,
            IsMandatory = evidenceType.IsMandatory,
            Category = evidenceType.Category
        };
    }

    public async Task DeleteEvidenceTypeAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var evidenceType = await _unitOfWork.EvidenceTypeRepository.GetByIdAsync(id, cancellationToken);
        if (evidenceType == null) throw new KeyNotFoundException("EvidenceType not found.");

        var inUse = (await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken))
            .Any(e => e.EvidenceTypeId == id && !e.IsDeleted);
        if (inUse)
        {
            throw new BusinessRuleViolationException($"Không thể xóa loại bằng chứng '{evidenceType.TypeName}' vì đang có hồ sơ/file bằng chứng liên kết.");
        }

        evidenceType.IsDeleted = true;
        evidenceType.DeletedAt = DateTime.UtcNow;
        evidenceType.UpdatedAt = DateTime.UtcNow;
        evidenceType.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.EvidenceTypeRepository.Update(evidenceType);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = deletedByAccountId,
            ActionType = AuditActionType.DELETE.ToString(),
            EntityName = nameof(EvidenceType),
            RecordId = evidenceType.EvidenceTypeId,
            OldValue = evidenceType.TypeName,
            NewValue = "Deleted",
            Description = $"EvidenceType #{evidenceType.EvidenceTypeId} ({evidenceType.TypeName}) soft-deleted"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
