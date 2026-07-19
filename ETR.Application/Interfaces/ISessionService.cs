using ETR.Application.DTOs.Session;

namespace ETR.Application.Interfaces;

public interface ISessionService
{
    Task<IEnumerable<SessionResponse>> GetAllSessionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionResponse>> GetSessionsByClassIdAsync(int classId, CancellationToken cancellationToken = default);
    Task<SessionResponse> GetSessionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SessionResponse> CreateSessionAsync(CreateSessionRequest request, int createdByAccountId, CancellationToken cancellationToken = default);
    Task<SessionResponse> UpdateSessionAsync(int id, UpdateSessionRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
