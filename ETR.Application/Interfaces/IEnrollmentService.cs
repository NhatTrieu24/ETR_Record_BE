using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IEnrollmentService
{
    Task<IEnumerable<EnrollmentResponse>> GetAllEnrollmentsAsync(CancellationToken cancellationToken = default);
    Task<EnrollmentResponse> GetEnrollmentByIdAsync(int enrollmentId, CancellationToken cancellationToken = default);
    
    Task<CreateEnrollmentResponse> CreateEnrollmentAsync(
        int accountId,
        int classId,
        int createdByAccountId,
        CancellationToken cancellationToken = default);
}
