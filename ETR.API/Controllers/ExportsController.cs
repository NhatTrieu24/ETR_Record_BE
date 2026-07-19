using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: System Auditing &amp; Compliance
/// [Core Responsibility]: Triggers and retrieves data export jobs.
/// [Target Audience]: Admin
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExportsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ExportsController(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// [Module/Flow]: System Auditing &amp; Compliance
    /// [Core Responsibility]: Retrieves a specific export job by ID.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExportJobResponse>> GetExportJob(int id, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.ExportJobRepository.GetByIdAsync(id, cancellationToken);
        if (job == null) return NotFound($"Không tìm thấy yêu cầu xuất dữ liệu với ID {id}.");
        return Ok(MapJobToResponse(job));
    }

    /// <summary>
    /// [Module/Flow]: System Auditing &amp; Compliance
    /// [Core Responsibility]: Triggers an export job for a training package.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpPost("training-package")]
    public async Task<ActionResult<ExportJobResponse>> ExportTrainingPackage([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        return await CreateMockExportJob("TrainingPackage", accountId, cancellationToken);
    }

    /// <summary>
    /// [Module/Flow]: System Auditing &amp; Compliance
    /// [Core Responsibility]: Triggers an export job for a PDF report.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpPost("pdf")]
    public async Task<ActionResult<ExportJobResponse>> ExportPdf([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        return await CreateMockExportJob("PDF", accountId, cancellationToken);
    }

    /// <summary>
    /// [Module/Flow]: System Auditing &amp; Compliance
    /// [Core Responsibility]: Triggers an export job for a dashboard summary.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpPost("dashboard")]
    public async Task<ActionResult<ExportJobResponse>> ExportDashboard([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        return await CreateMockExportJob("Dashboard", accountId, cancellationToken);
    }

    /// <summary>
    /// [Module/Flow]: System Auditing &amp; Compliance
    /// [Core Responsibility]: Downloads the generated file of a completed export job.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpGet("download/{id:int}")]
    public async Task<IActionResult> DownloadExportFile(int id, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.ExportJobRepository.GetByIdAsync(id, cancellationToken);
        if (job == null) return NotFound($"Không tìm thấy yêu cầu xuất dữ liệu với ID {id}.");
        if (job.Status != "Completed") return BadRequest("File xuất chưa hoàn thành hoặc đã bị lỗi.");

        var mockFileContent = $"Nội dung tệp xuất loại: {job.ExportType}, Tên tệp: {job.FileName}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(mockFileContent);
        var stream = new System.IO.MemoryStream(bytes);

        return File(stream, "application/octet-stream", job.FileName);
    }

    private async Task<ActionResult<ExportJobResponse>> CreateMockExportJob(string exportType, int requestedBy, CancellationToken cancellationToken)
    {
        var job = new ExportJob
        {
            RequestedByAccountId = requestedBy,
            ExportType = exportType,
            FileName = $"{exportType.ToLower()}_export_{DateTime.UtcNow:yyyyMMddHHmmss}.zip",
            FilePath = $"/exports/{Guid.NewGuid()}",
            Status = "Completed", // Complete synchronously for testing convenience
            RequestedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            DownloadExpiredAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.ExportJobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Ok(MapJobToResponse(job));
    }

    private static ExportJobResponse MapJobToResponse(ExportJob j)
    {
        return new ExportJobResponse(
            j.ExportJobId,
            j.RequestedByAccountId,
            j.ExportType,
            j.FileName,
            j.FilePath,
            j.Status,
            j.RequestedAt,
            j.CompletedAt,
            j.DownloadExpiredAt);
    }
}


