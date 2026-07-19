using ETR.Application.DTOs.EvidenceType;

namespace ETR.Application.Interfaces;

public interface IEvidenceTypeService
{
    Task<IEnumerable<EvidenceTypeResponse>> GetAllEvidenceTypesAsync(CancellationToken cancellationToken = default);
    Task<EvidenceTypeResponse> GetEvidenceTypeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EvidenceTypeResponse> CreateEvidenceTypeAsync(CreateEvidenceTypeRequest request, int createdByAccountId, CancellationToken cancellationToken = default);
    Task<EvidenceTypeResponse> UpdateEvidenceTypeAsync(int id, UpdateEvidenceTypeRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteEvidenceTypeAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
