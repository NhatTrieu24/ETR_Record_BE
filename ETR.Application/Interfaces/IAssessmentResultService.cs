using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IAssessmentResultService
{
    Task<IEnumerable<AssessmentResultResponse>> GetAllAssessmentResultsAsync(CancellationToken cancellationToken = default);
    Task<AssessmentResultResponse> GetAssessmentResultByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AssessmentResultResponse>> GetAssessmentResultsByClassStudentAsync(int classStudentId, int accountId, string? roleName, CancellationToken cancellationToken = default);
    Task<AssessmentResultResponse> RecordAssessmentScoreAsync(CreateAssessmentResultRequest request, int recordedByAccountId, CancellationToken cancellationToken = default);
    Task<AssessmentResultResponse> UpdateAssessmentResultAsync(int id, UpdateAssessmentResultRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task<AssessmentResultResponse> PublishAssessmentResultAsync(int id, int publishedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteAssessmentResultAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
    Task<SubjectSignoffResponse> SignoffSubjectResultAsync(CreateSubjectSignoffRequest request, int signoffByAccountId, string signoffByRoleName, CancellationToken cancellationToken = default);
}
