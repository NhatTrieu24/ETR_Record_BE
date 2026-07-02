using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ETR.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AuditLog entity, CancellationToken cancellationToken = default)
    {
        await _context.AuditLogs.AddAsync(entity, cancellationToken);
    }
}
