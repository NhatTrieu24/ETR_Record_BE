using ETR.Application.DTOs.CompletionRequirement;

namespace ETR.Application.Interfaces;

public interface ICompletionRequirementService
{
    Task<IEnumerable<CompletionRequirementResponse>> GetAllCompletionRequirementsAsync(CancellationToken cancellationToken = default);
    Task<CompletionRequirementResponse> GetCompletionRequirementByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CompletionRequirementResponse>> GetCompletionRequirementsByCourseAsync(int courseId, CancellationToken cancellationToken = default);
    Task<CompletionRequirementResponse> CreateCompletionRequirementAsync(CreateCompletionRequirementRequest request, int createdByAccountId, CancellationToken cancellationToken = default);
    Task<CompletionRequirementResponse> UpdateCompletionRequirementAsync(int id, UpdateCompletionRequirementRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteCompletionRequirementAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
