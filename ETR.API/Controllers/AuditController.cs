using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Kiểm toán Hệ thống &amp; Tuân thủ
/// [Core Responsibility]: Retrieves system audit logs for administrative tracking.
/// [Target Audience]: Admin
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// [Module/Flow]: Kiểm toán Hệ thống &amp; Tuân thủ
    /// [Core Responsibility]: Lấy toàn bộ nhật ký hệ thống (audit logs).
    /// [Target Audience]: Admin
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AuditLogResponse>>> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var list = await _unitOfWork.AuditLogRepository.GetAllAsync(cancellationToken);
        var ordered = list.OrderByDescending(l => l.AuditLogId).ToList();
        var pageItems = ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(MapLogToResponse);
        return Ok(new PagedResponse<AuditLogResponse>(pageItems, ordered.Count, page, pageSize));
    }

    /// <summary>
    /// [Module/Flow]: Kiểm toán Hệ thống &amp; Tuân thủ
    /// [Core Responsibility]: Lấy thông tin một audit log cụ thể theo ID.
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
    /// [Module/Flow]: Kiểm toán Hệ thống &amp; Tuân thủ
    /// [Core Responsibility]: Tìm kiếm audit logs theo loại hành động, tên thực thể hoặc mô tả.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<PagedResponse<AuditLogResponse>>> SearchAuditLogs([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var logs = await _unitOfWork.AuditLogRepository.GetAllAsync(cancellationToken);
        var filtered = logs.Where(l => l.ActionType.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                       l.EntityName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                       (l.Description != null && l.Description.Contains(query, StringComparison.OrdinalIgnoreCase)))
                             .OrderByDescending(l => l.AuditLogId)
                             .ToList();
        var pageItems = filtered.Skip((page - 1) * pageSize).Take(pageSize).Select(MapLogToResponse);
        return Ok(new PagedResponse<AuditLogResponse>(pageItems, filtered.Count, page, pageSize));
    }

    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
    {
        return (Math.Max(page, 1), Math.Clamp(pageSize, 1, 100));
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


