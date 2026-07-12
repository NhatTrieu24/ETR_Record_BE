using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;

    public ApprovalsController(IApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    [HttpPost("{id}/process")]
    public async Task<IActionResult> ProcessApproval(int id, [FromQuery] string action, [FromQuery] int userId, [FromQuery] string? comment, CancellationToken cancellationToken)
    {
        var response = await _approvalService.ProcessApprovalActionAsync(id, action, userId, comment, cancellationToken);
        return Ok(response);
    }
}
