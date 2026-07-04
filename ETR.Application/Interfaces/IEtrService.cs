using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IEtrService
{
    Task<EtrRecordResponse> SubmitEtrAsync(int etrCourseRecordId, int userId, CancellationToken cancellationToken = default);
    Task<EtrRecordResponse> VerifyEtrAsync(int etrCourseRecordId, int userId, CancellationToken cancellationToken = default);
    Task<EtrRecordResponse> CompleteEtrAsync(int etrCourseRecordId, int userId, CancellationToken cancellationToken = default);
    Task<EtrRecordResponse> LockEtrAsync(int etrCourseRecordId, int userId, CancellationToken cancellationToken = default);
    Task<EtrRecordResponse> UnlockEtrAsync(int etrCourseRecordId, int userId, CancellationToken cancellationToken = default);
}
