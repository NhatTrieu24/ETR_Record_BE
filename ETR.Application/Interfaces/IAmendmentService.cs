using ETR.Application.DTOs.Amendment;
using ETR.Application.DTOs.Amendment.Requests;

namespace ETR.Application.Interfaces;

public interface IAmendmentService
{
    Task<IEnumerable<AmendmentRequestResponse>> GetAllAmendmentRequestsAsync(CancellationToken cancellationToken = default);
    Task<AmendmentRequestResponse> GetAmendmentRequestByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AmendmentRequestResponse> CreateAmendmentRequestAsync(int subjectResultId, CreateAmendmentRequestRequest request, int requestedByAccountId, CancellationToken cancellationToken = default);
    Task<AmendmentRequestResponse> ApproveAmendmentRequestAsync(int id, DecideAmendmentRequestRequest request, int approvedByAccountId, CancellationToken cancellationToken = default);
    Task<AmendmentRequestResponse> RejectAmendmentRequestAsync(int id, DecideAmendmentRequestRequest request, int rejectedByAccountId, CancellationToken cancellationToken = default);
}
