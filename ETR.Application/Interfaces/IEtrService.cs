using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IEtrService
{
    Task<IEnumerable<EtrRecordResponse>> GetAllEtrsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<EtrRecordResponse>> GetMyEtrsAsync(int accountId, CancellationToken cancellationToken = default);
    Task<EtrDetailsResponse> GetEtrByIdAsync(int etrCourseRecordId, CancellationToken cancellationToken = default);
    Task DeleteEtrAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
    
    Task<EtrRecordResponse> SubmitEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default);
    Task<EtrRecordResponse> VerifyEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default);
    Task<EtrRecordResponse> ReturnEtrAsync(int etrCourseRecordId, int accountId, string? comment, CancellationToken cancellationToken = default);
    Task<EtrRecordResponse> CompleteEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default);
    Task<EtrRecordResponse> LockEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default);
    Task<EtrRecordResponse> UnlockEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default);
}
