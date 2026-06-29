using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IEtrService
{
    Task<ChecklistProgressResponse> UpdateChecklistProgressAsync(
        int progressId,
        bool isCompleted,
        int? verifiedByUserId,
        string? comment,
        CancellationToken cancellationToken = default);
    Task<EtrRecordResponse> SubmitEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default);
    Task<EtrRecordResponse> VerifyEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default);
    Task<EtrRecordResponse> CompleteEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default);
    Task<EtrRecordResponse> LockEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default);
    Task<EtrRecordResponse> UnlockEtrAsync(int etrRecordId, int userId, CancellationToken cancellationToken = default);
}
