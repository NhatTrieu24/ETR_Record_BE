using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IEnrollmentService
{
    Task<CreateEnrollmentResponse> CreateEnrollmentAsync(
        int accountId,
        int classId,
        int createdByAccountId,
        CancellationToken cancellationToken = default);
}
