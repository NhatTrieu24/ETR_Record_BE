using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EtrController : ControllerBase
{
    private readonly IEtrService _etrService;

    public EtrController(IEtrService etrService)
    {
        _etrService = etrService;
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> SubmitEtr(int id, [FromQuery] int userId, CancellationToken cancellationToken)
    {
        var response = await _etrService.SubmitEtrAsync(id, userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id}/verify")]
    public async Task<IActionResult> VerifyEtr(int id, [FromQuery] int userId, CancellationToken cancellationToken)
    {
        var response = await _etrService.VerifyEtrAsync(id, userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteEtr(int id, [FromQuery] int userId, CancellationToken cancellationToken)
    {
        var response = await _etrService.CompleteEtrAsync(id, userId, cancellationToken);
        return Ok(response);
    }
}