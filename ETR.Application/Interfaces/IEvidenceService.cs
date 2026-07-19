using ETR.Application.DTOs.Evidence;
using ETR.Application.DTOs.Evidence.Requests;

namespace ETR.Application.Interfaces;

public interface IEvidenceService
{
    Task<IEnumerable<EvidenceResponse>> GetAllEvidencesAsync(CancellationToken cancellationToken = default);
    Task<EvidenceResponse> GetEvidenceByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EvidenceResponse> UploadEvidenceAsync(UploadEvidenceRequest request, int uploadedByAccountId, string webRootPath, CancellationToken cancellationToken = default);
    Task<EvidenceResponse> VerifyEvidenceAsync(int id, VerifyEvidenceRequest request, int verifiedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteEvidenceAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
