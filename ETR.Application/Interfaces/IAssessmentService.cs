using ETR.Application.DTOs.Assessment.Requests;
using ETR.Application.DTOs.Assessment.Responses;

namespace ETR.Application.Interfaces;

public interface IAssessmentService
{
    Task<IEnumerable<AssessmentResponse>> GetAllAssessmentsAsync(CancellationToken cancellationToken = default);
    Task<AssessmentResponse> GetAssessmentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AssessmentResponse> CreateAssessmentAsync(CreateAssessmentRequest request, int createdByAccountId, CancellationToken cancellationToken = default);
    Task<AssessmentResponse> UpdateAssessmentAsync(int id, UpdateAssessmentRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteAssessmentAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
