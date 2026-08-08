using ETR.Application.DTOs;
using ETR.Application.DTOs.Approval;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Xử lý ETR
/// [Core Responsibility]: Processes approval workflows and state transitions for ETR records.
/// [Target Audience]: Admin, Instructor
/// </summary>
[ApiController]
[Route("api/[controller]")]
// NOTE: ASP.NET Core combines class-level and method-level [Authorize(Roles=...)] via AND, not OR —
// every role a method attribute grants (e.g. QA on ProcessApproval below) MUST also appear here, or
// the method-level grant is silently voided. Keep this list a superset of every method's role list.
[Authorize(Roles = "Admin,Instructor,QA,TrainingManager,Audit")]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly ICurrentUserService _currentUserService;

    public ApprovalsController(IApprovalService approvalService, ICurrentUserService currentUserService)
    {
        _approvalService = approvalService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllApprovalRequests(CancellationToken cancellationToken)
    {
        var requests = await _approvalService.GetAllApprovalRequestsAsync(cancellationToken);
        return Ok(requests);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Instructor,TrainingManager")]
    public async Task<IActionResult> CreateApprovalRequest([FromBody] CreateApprovalRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _approvalService.CreateApprovalRequestAsync(request.ETRCourseRecordId, request.CurrentApproverId, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// [Module/Flow]: Xử lý ETR — Approval Workflow
    /// [Core Responsibility]: Chuẩn hóa `action` thành enum `ApprovalActionType` (Verify/Approve/
    /// Reject/Return) thay vì chuỗi tự do — giá trị không hợp lệ bị model binding chặn với 400 trước
    /// khi chạm tới Service; Swagger/OpenAPI cũng nhờ đó phát sinh đúng kiểu enum cho FE.
    /// [Target Audience]: QA (Verify/Reject/Return), TrainingManager (Approve), Admin (mọi action)
    /// </summary>
    [HttpPost("{id}/process")]
    [Authorize(Roles = "Admin,QA,TrainingManager")]
    public async Task<IActionResult> ProcessApproval(int id, [FromQuery] ApprovalActionType action, [FromQuery] string? comment, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _approvalService.ProcessApprovalActionAsync(id, action, accountId, _currentUserService.RoleName, comment, cancellationToken);
        return Ok(response);
    }
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Instructor,TrainingManager")]
    public async Task<IActionResult> UpdateApprovalRequest(int id, [FromBody] UpdateApprovalRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _approvalService.UpdateApprovalRequestAsync(id, request, accountId, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Instructor,TrainingManager")]
    public async Task<IActionResult> DeleteApprovalRequest(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        await _approvalService.DeleteApprovalRequestAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}


