using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Kiểm toán Hệ thống &amp; Tuân thủ
/// [Core Responsibility]: Triggers and retrieves data export jobs.
/// [Target Audience]: Admin
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Audit,Academic")]
public class ExportsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IExportService _exportService;
    private readonly IWebHostEnvironment _env;

    public ExportsController(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IExportService exportService, IWebHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _exportService = exportService;
        _env = env;
    }

    /// <summary>
    /// [Module/Flow]: Kiểm toán Hệ thống &amp; Tuân thủ
    /// [Core Responsibility]: Lấy thông tin một công việc xuất tệp (export job) cụ thể theo ID.
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
    /// [Module/Flow]: Kiểm toán Hệ thống &amp; Tuân thủ
    /// [Core Responsibility]: Kích hoạt một công việc xuất tệp cho gói đào tạo (training package).
    /// [Target Audience]: Admin
    /// </summary>
    [HttpPost("training-package")]
    public async Task<ActionResult<ExportJobResponse>> ExportTrainingPackage([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        if (request.ETRCourseRecordId is not int etrCourseRecordId)
        {
            return BadRequest("ETRCourseRecordId is required to export a Training Package.");
        }

        var response = await _exportService.ExportTrainingPackageAsync(etrCourseRecordId, accountId, ResolveWebRootPath(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// [Module/Flow]: Kiểm toán Hệ thống &amp; Tuân thủ
    /// [Core Responsibility]: Kích hoạt một công việc xuất tệp PDF độc lập cho tóm tắt 1 hồ sơ ETR.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpPost("pdf")]
    public async Task<ActionResult<ExportJobResponse>> ExportPdf([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        if (request.ETRCourseRecordId is not int etrCourseRecordId)
        {
            return BadRequest("ETRCourseRecordId is required to export a standalone PDF summary.");
        }

        var response = await _exportService.ExportEtrPdfAsync(etrCourseRecordId, accountId, ResolveWebRootPath(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// [Module/Flow]: Kiểm toán Hệ thống &amp; Tuân thủ
    /// [Core Responsibility]: Kích hoạt một công việc xuất tệp PDF cho bản tóm tắt dashboard.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpPost("dashboard")]
    public async Task<ActionResult<ExportJobResponse>> ExportDashboard([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var response = await _exportService.ExportDashboardReportAsync(accountId, ResolveWebRootPath(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// [Module/Flow]: Kiểm toán Hệ thống &amp; Tuân thủ
    /// [Core Responsibility]: Kích hoạt một công việc xuất báo cáo điểm danh độc lập cho 1 lớp học.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpPost("attendance")]
    public async Task<ActionResult<ExportJobResponse>> ExportAttendanceReport([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        if (request.ClassId is not int classId)
        {
            return BadRequest("ClassId is required to export an Attendance report.");
        }

        var response = await _exportService.ExportAttendanceReportAsync(classId, accountId, ResolveWebRootPath(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// [Module/Flow]: Kiểm toán Hệ thống &amp; Tuân thủ
    /// [Core Responsibility]: Kích hoạt một công việc xuất báo cáo đánh giá độc lập cho 1 lớp học.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpPost("assessment")]
    public async Task<ActionResult<ExportJobResponse>> ExportAssessmentReport([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        if (request.ClassId is not int classId)
        {
            return BadRequest("ClassId is required to export an Assessment report.");
        }

        var response = await _exportService.ExportAssessmentReportAsync(classId, accountId, ResolveWebRootPath(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// [Module/Flow]: Kiểm toán Hệ thống &amp; Tuân thủ
    /// [Core Responsibility]: Kích hoạt một công việc xuất báo cáo Excel tổng hợp toàn bộ học viên
    /// trong 1 lớp/khoá (đa dòng), khác với báo cáo tóm tắt theo từng ETR riêng lẻ.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpPost("class-summary")]
    public async Task<ActionResult<ExportJobResponse>> ExportClassSummary([FromBody] ExportRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        if (request.ClassId is not int classId)
        {
            return BadRequest("ClassId is required to export a Class Summary report.");
        }

        var response = await _exportService.ExportClassSummaryReportAsync(classId, accountId, ResolveWebRootPath(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// [Module/Flow]: Kiểm toán Hệ thống &amp; Tuân thủ
    /// [Core Responsibility]: Tải xuống tệp đã được tạo từ một công việc xuất tệp hoàn tất.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpGet("download/{id:int}")]
    public async Task<IActionResult> DownloadExportFile(int id, CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.ExportJobRepository.GetByIdAsync(id, cancellationToken);
        if (job == null) return NotFound($"Không tìm thấy yêu cầu xuất dữ liệu với ID {id}.");
        if (job.Status != "Completed") return BadRequest("File xuất chưa hoàn thành hoặc đã bị lỗi.");

        if (!string.IsNullOrEmpty(job.FilePath))
        {
            var physicalPath = Path.Combine(ResolveWebRootPath(), job.FilePath);
            if (System.IO.File.Exists(physicalPath))
            {
                return PhysicalFile(physicalPath, "application/octet-stream", job.FileName);
            }
        }

        // Every export type now writes a real file to disk (see ExportService) — reaching here means
        // the file is missing (e.g. deleted out-of-band), not that the export type is still mocked.
        return NotFound($"Tệp xuất dữ liệu cho công việc #{id} không còn tồn tại trên đĩa.");
    }

    private string ResolveWebRootPath()
    {
        var webRootPath = _env.WebRootPath;
        return string.IsNullOrEmpty(webRootPath) ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot") : webRootPath;
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
            j.DownloadExpiredAt,
            j.ETRCourseRecordId);
    }
}


