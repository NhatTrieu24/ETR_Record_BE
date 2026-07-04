using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IEnrollmentService
{
    Task<CreateEnrollmentResponse> CreateEnrollmentAsync(
        int learnerId,
        int classId,
        int createdByUserId,
        CancellationToken cancellationToken = default);
}
