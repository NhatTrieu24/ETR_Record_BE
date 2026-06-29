using ETR.Domain.Entities;

namespace ETR.Application.Interfaces;

public interface IAuditLogRepository
{
    Task<AuditLog?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AuditLog entity, CancellationToken cancellationToken = default);
}
