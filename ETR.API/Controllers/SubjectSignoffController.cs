using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Ký xác nhận Subject
/// [Core Responsibility]: Instructor signs off subject results to confirm completion. Endpoint
/// duy nhất cho signoff — trước đây trùng với AssessmentResultsController.SignoffSubject (đã gộp,
/// xem P3 mục 36), role là hợp của cả 2 controller cũ để không thu hẹp quyền truy cập ai đó đang dùng.
/// [Target Audience]: Instructor, Academic, Admin
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Instructor,Academic,Admin")]
public class SubjectSignoffController : ControllerBase
{
    private readonly IAssessmentResultService _assessmentResultService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public SubjectSignoffController(
        IAssessmentResultService assessmentResultService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _assessmentResultService = assessmentResultService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Lấy danh sách tất cả các SubjectSignoff.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllSignoffs(CancellationToken cancellationToken)
    {
        var signoffs = await _unitOfWork.SubjectSignoffRepository.GetAllAsync(cancellationToken);
        return Ok(signoffs);
    }

    /// <summary>
    /// Instructor ký xác nhận (Subject Signoff) cho một Subject Result.
    /// Sau khi ký, hệ thống tự động đánh giá Pass/Fail dựa trên:
    /// - AttendanceRate >= 80%
    /// - PracticalChecklist hoàn thành
    /// - Evidence đã tải lên
    /// - Score >= PassingScore
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SignoffSubjectResult(
        [FromBody] CreateSubjectSignoffRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        var roleName = _currentUserService.RoleName
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var result = await _assessmentResultService.SignoffSubjectResultAsync(request, accountId, roleName, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Kiểm tra trạng thái signoff của một Subject Result.
    /// </summary>
    [HttpGet("check/{subjectResultId:int}")]
    public async Task<IActionResult> CheckSubjectSignoff(
        int subjectResultId,
        CancellationToken cancellationToken)
    {
        var allSignoffs = await _unitOfWork.SubjectSignoffRepository.GetAllAsync(cancellationToken);
        var signoff = allSignoffs.FirstOrDefault(s => s.SubjectResultId == subjectResultId);

        if (signoff == null)
            return Ok(new { IsSignedOff = false, Signoff = (object?)null });

        return Ok(new { IsSignedOff = true, Signoff = new SubjectSignoffResponse(
            signoff.SubjectSignoffId,
            signoff.SubjectResultId,
            signoff.SignoffByAccountId,
            signoff.Role,
            signoff.SignoffAt,
            signoff.Comment) });
    }
}
