using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: System Auditing &amp; Compliance
/// [Core Responsibility]: Retrieves system audit logs for administrative tracking.
/// [Target Audience]: Admin
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// [Module/Flow]: System Auditing &amp; Compliance
    /// [Core Responsibility]: Retrieves all audit logs.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogResponse>>> GetAuditLogs(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.AuditLogRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapLogToResponse));
    }

    /// <summary>
    /// [Module/Flow]: System Auditing &amp; Compliance
    /// [Core Responsibility]: Retrieves a specific audit log by ID.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<AuditLogResponse>> GetAuditLogById(long id, CancellationToken cancellationToken)
    {
        var log = await _unitOfWork.AuditLogRepository.GetByIdAsync(id, cancellationToken);
        if (log == null) return NotFound($"Không tìm thấy nhật ký kiểm toán với ID {id}.");
        return Ok(MapLogToResponse(log));
    }

    /// <summary>
    /// [Module/Flow]: System Auditing &amp; Compliance
    /// [Core Responsibility]: Searches audit logs by action type, entity name, or description.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<AuditLogResponse>>> SearchAuditLogs([FromQuery] string query, CancellationToken cancellationToken)
    {
        var logs = await _unitOfWork.AuditLogRepository.GetAllAsync(cancellationToken);
        var filtered = logs.Where(l => l.ActionType.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                                       l.EntityName.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                                       (l.Description != null && l.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));
        return Ok(filtered.Select(MapLogToResponse));
    }

    private static AuditLogResponse MapLogToResponse(AuditLog l)
    {
        return new AuditLogResponse(
            l.AuditLogId,
            l.AccountId,
            l.ETRRecordId,
            l.ActionType,
            l.EntityName,
            l.RecordId,
            l.OldValue,
            l.NewValue,
            l.Description,
            l.IPAddress,
            l.UserAgent);
    }
}
