using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface ISubjectService
{
    Task<IEnumerable<SubjectResponse>> GetAllSubjectsAsync(CancellationToken cancellationToken = default);
    Task<SubjectResponse> GetSubjectByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SubjectResponse> CreateSubjectAsync(CreateSubjectRequest request, int createdByAccountId, CancellationToken cancellationToken = default);
    Task<SubjectResponse> UpdateSubjectAsync(int id, UpdateSubjectRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteSubjectAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
