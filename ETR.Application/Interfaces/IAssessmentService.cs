using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IAssessmentService
{
    Task<AssessmentResultResponse> RecordAssessmentScoreAsync(CreateAssessmentResultRequest request, int recordedByUserId, CancellationToken cancellationToken = default);
    Task<SubjectSignoffResponse> SignoffSubjectResultAsync(CreateSubjectSignoffRequest request, int signoffByUserId, CancellationToken cancellationToken = default);
}
