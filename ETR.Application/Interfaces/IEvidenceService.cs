using ETR.Application.DTOs.Evidence;

namespace ETR.Application.Interfaces;

public interface IEvidenceService
{
    Task<IEnumerable<EvidenceResponse>> GetAllEvidencesAsync(CancellationToken cancellationToken = default);
    Task<EvidenceResponse> GetEvidenceByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EvidenceResponse> UploadEvidenceAsync(CreateEvidenceRequest request, int uploadedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteEvidenceAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
