using ETR.Application.DTOs;
using ETR.Domain.Entities;

namespace ETR.Application.Interfaces;

public interface IEnrollmentService
{
    Task<CreateEnrollmentResponse> CreateEnrollmentAsync(
        int learnerId,
        int classId,
        int createdByUserId,
        CancellationToken cancellationToken = default);
}
