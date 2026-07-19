using ETR.Application.DTOs.PracticalChecklist;

namespace ETR.Application.Interfaces;

public interface IPracticalChecklistService
{
    Task<IEnumerable<PracticalChecklistResponse>> GetAllPracticalChecklistsAsync(CancellationToken cancellationToken = default);
    Task<PracticalChecklistResponse> GetPracticalChecklistByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PracticalChecklistResponse>> GetPracticalChecklistsBySubjectAsync(int courseId, int subjectId, CancellationToken cancellationToken = default);
    Task<PracticalChecklistResponse> CreatePracticalChecklistAsync(CreatePracticalChecklistRequest request, int createdByAccountId, CancellationToken cancellationToken = default);
    Task<PracticalChecklistResponse> UpdatePracticalChecklistAsync(int id, UpdatePracticalChecklistRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeletePracticalChecklistAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
