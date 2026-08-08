using ETR.Application.DTOs.Amendment.Requests;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Amendment / Unlock-request cấp SubjectSignoff
/// [Core Responsibility]: Training Manager xét duyệt các yêu cầu "mở khóa" SubjectResult đã
/// Sign-off (tạo bởi SubjectSignoffController.RequestUnlock) — Approve reopen SubjectResult về
/// "Pending" và vô hiệu hóa chữ ký cũ; Reject giữ nguyên trạng thái, chỉ ghi lại lý do từ chối.
/// [Target Audience]: Instructor/Academic (xem danh sách), TrainingManager/Admin (duyệt)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AmendmentsController : ControllerBase
{
    private readonly IAmendmentService _amendmentService;
    private readonly ICurrentUserService _currentUserService;

    public AmendmentsController(IAmendmentService amendmentService, ICurrentUserService currentUserService)
    {
        _amendmentService = amendmentService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Lấy danh sách tất cả Amendment Request.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Instructor,Academic,TrainingManager,Admin")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var requests = await _amendmentService.GetAllAmendmentRequestsAsync(cancellationToken);
        return Ok(requests);
    }

    /// <summary>
    /// Lấy chi tiết một Amendment Request theo ID.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Instructor,Academic,TrainingManager,Admin")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var request = await _amendmentService.GetAmendmentRequestByIdAsync(id, cancellationToken);
        return Ok(request);
    }

    /// <summary>
    /// Training Manager duyệt Amendment Request — SubjectResult liên quan trở về "Pending" để
    /// Instructor sửa lại và Sign-off lần nữa; chữ ký cũ bị vô hiệu hóa (soft-delete).
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "TrainingManager,Admin")]
    public async Task<IActionResult> Approve(int id, [FromBody] DecideAmendmentRequestRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _amendmentService.ApproveAmendmentRequestAsync(id, request, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Training Manager từ chối Amendment Request — SubjectResult giữ nguyên trạng thái đã ký,
    /// bắt buộc phải có lý do từ chối (Comment).
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "TrainingManager,Admin")]
    public async Task<IActionResult> Reject(int id, [FromBody] DecideAmendmentRequestRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _amendmentService.RejectAmendmentRequestAsync(id, request, accountId, cancellationToken);
        return Ok(response);
    }
}
