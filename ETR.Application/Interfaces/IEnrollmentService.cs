using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IEnrollmentService
{
    Task<IEnumerable<EnrollmentResponse>> GetAllEnrollmentsAsync(CancellationToken cancellationToken = default);
    Task<EnrollmentResponse> GetEnrollmentByIdAsync(int enrollmentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EnrollmentResponse>> GetEnrollmentsByStudentIdAsync(int studentId, CancellationToken cancellationToken = default);
    
    Task<CreateEnrollmentResponse> CreateEnrollmentAsync(
        int accountId,
        int classId,
        int createdByAccountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Same as <see cref="CreateEnrollmentAsync"/> but assumes the caller has already opened a
    /// transaction (via <c>IUnitOfWork.BeginTransactionAsync</c>) and will commit/rollback it —
    /// used by bulk-import flows that need to enroll several students atomically alongside other
    /// writes in one transaction. Does not begin, commit, or roll back a transaction itself.
    /// </summary>
    Task<CreateEnrollmentResponse> CreateEnrollmentCoreAsync(
        int accountId,
        int classId,
        int createdByAccountId,
        CancellationToken cancellationToken = default);

    Task<EnrollmentResponse> UpdateEnrollmentAsync(int id, UpdateEnrollmentRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteEnrollmentAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
