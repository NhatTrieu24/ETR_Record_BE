using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IAssessmentService
{
    Task<IEnumerable<AssessmentResultResponse>> GetAllAssessmentResultsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AssessmentResultResponse>> GetAssessmentResultsByClassStudentAsync(int classStudentId, int accountId, CancellationToken cancellationToken = default);
    Task<AssessmentResultResponse> RecordAssessmentScoreAsync(CreateAssessmentResultRequest request, int recordedByAccountId, CancellationToken cancellationToken = default);
    Task<SubjectSignoffResponse> SignoffSubjectResultAsync(CreateSubjectSignoffRequest request, int signoffByAccountId, CancellationToken cancellationToken = default);
}
