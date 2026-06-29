using ETR.Domain.Entities;

namespace ETR.Application.Interfaces;

public interface IEtrService
{
    Task<ETRChecklistProgress> UpdateChecklistProgressAsync(
        int progressId,
        bool isCompleted,
        int? verifiedByUserId,
        string? comment,
        CancellationToken cancellationToken = default);
    Task<ETRRecord> SubmitEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default);
    Task<ETRRecord> VerifyEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default);
    Task<ETRRecord> CompleteEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default);
}
