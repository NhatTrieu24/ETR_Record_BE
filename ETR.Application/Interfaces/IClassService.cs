using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IClassService
{
    Task<IEnumerable<TrainingClassResponse>> GetAllClassesAsync(CancellationToken cancellationToken = default);
    Task<TrainingClassResponse> GetClassByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TrainingClassResponse> CreateClassAsync(CreateClassRequest request, int createdByAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Same as <see cref="CreateClassAsync"/> but assumes the caller has already opened a
    /// transaction (via <c>IUnitOfWork.BeginTransactionAsync</c>) and will commit/rollback it —
    /// used by bulk-import flows that need to create several classes atomically alongside other
    /// writes in one transaction. Does not begin, commit, or roll back a transaction itself.
    /// </summary>
    Task<TrainingClassResponse> CreateClassCoreAsync(CreateClassRequest request, int createdByAccountId, CancellationToken cancellationToken = default);
    Task<TrainingClassResponse> UpdateClassAsync(int id, UpdateClassRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteClassAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
