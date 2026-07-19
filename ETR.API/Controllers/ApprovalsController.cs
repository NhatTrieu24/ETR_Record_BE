using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: ETR Processing
/// [Core Responsibility]: Processes approval workflows and state transitions for ETR records.
/// [Target Audience]: Admin, Instructor
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly ICurrentUserService _currentUserService;

    public ApprovalsController(IApprovalService approvalService, ICurrentUserService currentUserService)
    {
        _approvalService = approvalService;
        _currentUserService = currentUserService;
    }

    [HttpPost("{id}/process")]
    public async Task<IActionResult> ProcessApproval(int id, [FromQuery] string action, [FromQuery] string? comment, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _approvalService.ProcessApprovalActionAsync(id, action, accountId, comment, cancellationToken);
        return Ok(response);
    }
}


