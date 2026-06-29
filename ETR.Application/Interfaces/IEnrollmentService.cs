using ETR.Domain.Entities;

namespace ETR.Application.Interfaces;

public record CreateEnrollmentResult(Enrollment Enrollment, ETRRecord ETRRecord);

public interface IEnrollmentService
{
    Task<CreateEnrollmentResult> CreateEnrollmentAsync(
        int learnerId,
        int classId,
        int createdByUserId,
        CancellationToken cancellationToken = default);
}
