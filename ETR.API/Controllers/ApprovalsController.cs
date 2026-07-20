using ETR.Application.DTOs;
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
[Authorize(Roles = "Admin,Instructor,TrainingManager")]
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
    public async Task<IActionResult> CreateApprovalRequest([FromBody] CreateApprovalRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _approvalService.CreateApprovalRequestAsync(request.ETRCourseRecordId, request.CurrentApproverId, accountId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id}/process")]
    public async Task<IActionResult> ProcessApproval(int id, [FromQuery] string action, [FromQuery] string? comment, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _approvalService.ProcessApprovalActionAsync(id, action, accountId, comment, cancellationToken);
        return Ok(response);
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApprovalRequest(int id, [FromBody] UpdateApprovalRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _approvalService.UpdateApprovalRequestAsync(id, request, accountId, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApprovalRequest(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        await _approvalService.DeleteApprovalRequestAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}


