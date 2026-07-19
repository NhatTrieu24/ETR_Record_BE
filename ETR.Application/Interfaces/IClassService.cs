using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IClassService
{
    Task<IEnumerable<TrainingClassResponse>> GetAllClassesAsync(CancellationToken cancellationToken = default);
    Task<TrainingClassResponse> GetClassByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TrainingClassResponse> CreateClassAsync(CreateClassRequest request, int createdByAccountId, CancellationToken cancellationToken = default);
    Task<TrainingClassResponse> UpdateClassAsync(int id, UpdateClassRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteClassAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
