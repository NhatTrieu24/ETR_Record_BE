using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IAssessmentService
{
    Task<AssessmentResultResponse> RecordAssessmentScoreAsync(CreateAssessmentResultRequest request, CancellationToken cancellationToken = default);
    Task<SubjectSignoffResponse> SignoffSubjectResultAsync(CreateSubjectSignoffRequest request, CancellationToken cancellationToken = default);
}
